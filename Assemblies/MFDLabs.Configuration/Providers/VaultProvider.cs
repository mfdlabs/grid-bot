using System;
using System.Text;
using System.Linq;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using MFDLabs.Threading;
using MFDLabs.Configuration.Sections.Vault;
using MFDLabs.Configuration.Clients.Vault;
using MFDLabs.Configuration.Logging;
using MFDLabs.Text.Extensions;
using MFDLabs.Configuration.Settings;
using MFDLabs.Configuration.Elements.Vault;

namespace MFDLabs.Configuration.Providers
{
    public class VaultProvider : SettingsProvider
    {
        static VaultProvider()
        {
            ConfigurationLogging.Info("MFDLabs.Configuration.Providers.VaultProvider static init started.");
            _configurationSection = ConfigurationManager.GetSection("mfdlabsVaultConfiguration") as VaultProviderConfigurationSection;
            if (_configurationSection != null)
            {
                var configuration = GetGroupConfigurationElement();
                var address = _address = configuration.Address;
                _configurationClient = new VaultConfigurationClient(address, configuration.Token);
                ConfigurationLogging.Info("MFDLabs.Configuration.Providers.VaultProvider static init Vault Client point to address '{0}'.", address);
                var updateInterval = configuration.UpdateInterval;
                _timer = new SelfDisposingTimer(RefreshRegisteredProviders, updateInterval, updateInterval);
                _providers = new ConcurrentDictionary<string, VaultProvider>();
                ConfigurationLogging.Info("MFDLabs.Configuration.Providers.VaultProvider static init Timer created, refresh every '{0}'.", updateInterval);
                return;
            }
            ConfigurationLogging.Warning("No config file found with mfdlabsVaultConfiguration.");
        }

        public static void Register(SettingsLoadedEventArgs e, ApplicationSettingsBase settings)
        {
            var provider = e.Provider as VaultProvider;
            if (provider != null)
            {
                provider.UpdateApplicationSettings(settings);
            }
            var groupName = provider?._groupName;
            if (!groupName.IsNullOrEmpty() && _providers != null && !_providers.ContainsKey(groupName))
            {
                if (_providers.TryAdd(groupName, provider))
                {
                    ConfigurationLogging.Info("Settings Group '{0}' is registered.", groupName);
                    return;
                }
                ConfigurationLogging.Info("Settings Group '{0}' failed to register.", groupName);
            }
        }

        private static void RefreshRegisteredProviders()
        {
            try
            {
                ConfigurationLogging.Info("RefreshRegisteredProviders - Start on {0} settings providers.", _providers.Count);
                _timer.Pause();
                foreach (var provider in _providers)
                {
                    provider.Value.ReloadChangedSettings();
                }
                ConfigurationLogging.Info("RefreshRegisteredProviders - Complete on {0} settings providers.", _providers.Count);
            }
            catch (Exception ex)
            {
                ConfigurationLogging.Error("RefreshRegisteredProviders in MFDLabs.Configuration.Providers.VaultProvider failed with the following\n {0}.", ex);
            }
            finally
            {
                _timer.Unpause();
            }
        }

        public override void Initialize(string name, NameValueCollection col)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = GetType().FullName;
            }
            base.Initialize(name, col);
        }

        private void UpdateApplicationSettings(ApplicationSettingsBase applicationSettings)
        {
            if (_appSettings != applicationSettings)
            {
                if (_appSettings != null)
                {
                    throw new InvalidOperationException("RegisterSettings changing applicationSettings");
                }
                _appSettings = applicationSettings;
            }
        }

        private void ReloadChangedSettings()
        {
            try
            {
                if (_appSettings == null)
                {
                    ConfigurationLogging.Warning("RegisterSettings in {0}.OnSettingsLoaded was not invoked. Setting changes made through the service will not be synchronized to this process.", _groupName);
                }
                else
                {
                    if (HasOverriddenSettingsFileBeenModified(out var lastModified))
                    {
                        _overriddenSettings = FileBasedSettingsOverrideHelper.ReadOverriddenSettingsFromFilePath(_overriddenSettingsFileName, null);
                        _lastFileBasedOverrideModDate = lastModified;
                        _appSettings.Reload();
                    }
                    if (_hasSettings || _hasConnectionStrings)
                    {
                        ConfigurationLogging.Info("Changes detected for Group {0}. Reloading settings.", _groupName);
                        _appSettings.Reload();
                    }
                }
            }
            catch (Exception ex)
            {
                ConfigurationLogging.Error(ex.ToString());
            }
        }

        private bool HasOverriddenSettingsFileBeenModified(out DateTime newModificationTime)
        {
            if (!string.IsNullOrWhiteSpace(_overriddenSettingsFileName))
            {
                try
                {
                    var lastWrite = File.GetLastWriteTime(_overriddenSettingsFileName);
                    newModificationTime = lastWrite;
                    return _lastFileBasedOverrideModDate < lastWrite;
                }
                catch (Exception ex)
                {
                    ConfigurationLogging.Warning("There was an exception while fetching last modification time for settings override filename:{0}. Exception:{1}", _overriddenSettingsFileName, ex);
                }
            }
            newModificationTime = _lastFileBasedOverrideModDate;
            return false;
        }

        public override string Description
        {
            get
            {
                return "A vault-backed SettingsProvider that synchronizes assembly settings from a common vault repository.";
            }
        }

        public override string ApplicationName { get; set; }

        private static VaultGroupConfigurationElement GetGroupConfigurationElement(string groupName = "*")
        {
            if (_configurationSection != null)
            {
                return _configurationSection.Groups[groupName] ?? _configurationSection.Groups["*"];
            }
            return null;
        }

        private void LoadLocalOverrides(SettingsPropertyCollection collection, out SettingsPropertyValueCollection settingsPropertyValueCollection, out Dictionary<string, SettingsPropertyValue> settingsProperties)
        {
            settingsPropertyValueCollection = new SettingsPropertyValueCollection();
            settingsProperties = new Dictionary<string, SettingsPropertyValue>(collection.Count);
            foreach (object obj in collection)
            {
                var settingsProperty = (SettingsProperty)obj;
                var value = new SettingsPropertyValue(settingsProperty);
                var name = string.Format("{0}.{1}", _groupName, settingsProperty.Name);
                if (TryGetOverriddenSettingValue(name, out var propertyValue))
                {
                    value.PropertyValue = propertyValue;
                }
                settingsPropertyValueCollection.Add(value);
                settingsProperties[settingsProperty.Name] = value;
            }
        }

        private void UpdateConnectionStringsFromVault(Dictionary<string, SettingsPropertyValue> settingsProperties, int maxAttempts, DateTime lastModification)
        {
            var allConnectionStrings = _configurationClient.FetchWithRetries<IConnectionString>(_configurationClient.GetAllConnectionStrings, _groupName, maxAttempts);
            _firstFetch = false;
            var connectionStrings = new List<string>();
            foreach (var connectionString in allConnectionStrings)
            {
                if (connectionString.Updated > lastModification)
                {
                    lastModification = connectionString.Updated;
                }
                if (!settingsProperties.TryGetValue(connectionString.Name, out var property))
                {
                    connectionStrings.Add(connectionString.Name);
                }
                else
                {
                    property.SerializedValue = connectionString.Value;
                }
            }
            if (connectionStrings.Count > 0)
            {
                ConfigurationLogging.Warning(BuildUnknownSettingsMessage(connectionStrings, "Connection Strings"));
            }
            _lastConnectionStringModDate = lastModification;
        }

        private void UpdateSettingsFromVault(Dictionary<string, SettingsPropertyValue> settingsProperties, int maxAttempts, DateTime lastModification)
        {
            var allSettings = _configurationClient.FetchWithRetries<ISetting>(_configurationClient.GetAllSettings, _groupName, maxAttempts);
            _firstFetch = false;
            List<string> settings = new List<string>();
            foreach (var setting in allSettings)
            {
                if (setting.Updated > lastModification)
                {
                    lastModification = setting.Updated;
                }
                if (!settingsProperties.TryGetValue(setting.Name, out var property))
                {
                    settings.Add(setting.Name);
                }
                else
                {
                    var name = string.Format("{0}.{1}", _groupName, setting.Name);
                    if (TryGetOverriddenSettingValue(name, out var value))
                    {
                        property.SerializedValue = value;
                    }
                    else
                    {
                        property.SerializedValue = setting.Value;
                    }
                }
            }
            if (settings.Count > 0)
            {
                ConfigurationLogging.Warning(BuildUnknownSettingsMessage(settings, "Settings"));
            }
            _lastSettingsModDate = lastModification;
        }

        private string BuildUnknownSettingsMessage(List<string> unknownSettings, string settingType)
        {
            var builder = new StringBuilder();
            builder.AppendLine(string.Format("The following unknown {0} are defined in group: {1}", settingType, _groupName));
            builder.AppendLine();
            int count = 0;
            foreach (var setting in unknownSettings)
            {
                if (builder.Length >= _softLogCharacterLimit)
                {
                    builder.Append(string.Format(" and {0} others. Message Truncated.", unknownSettings.Count - count));
                    break;
                }
                builder.Append(setting);
                builder.Append(", ");
                count++;
            }
            return builder.ToString();
        }

        private bool TryGetOverriddenSettingValue(string settingName, out object overriddenValue)
        {
            overriddenValue = null;
            if (_overriddenSettings != null && _overriddenSettings.TryGetValue(settingName, out object result))
            {
                overriddenValue = result;
                return true;
            }
            return false;
        }

        private void OneTimeInitializeFromContext(SettingsContext context)
        {
            ConfigurationLogging.Info("OneTimeInitializeFromContext from {0}", context);
            if (_oneTimeInitComplete)
            {
                return;
            }
            try
            {
                _groupName = (string)context["GroupName"];
            }
            catch (Exception ex)
            {
                throw new ConfigurationErrorsException("Invalid or missing GroupName", ex);
            }
            ConfigurationLogging.Info("ProviderBase identifying settings for {0}", _groupName);
            DetectSettingsAndConnectionStringInClass(context);
            var el = GetGroupConfigurationElement(_groupName);
            DetectFileSystemOverridesForGroup(el);
            if (string.IsNullOrEmpty(el?.Address))
            {
                DetectAlternateSettings();
            }
            _oneTimeInitComplete = true;
        }

        private void DetectAlternateSettings()
        {
            ConfigurationLogging.Info("Group {0} is using file-based configuration", _groupName);
            _alternateSettings = new LocalFileSettingsProvider();
            _alternateSettings.Initialize(null, null);
        }

        private void DetectSettingsAndConnectionStringInClass(SettingsContext context)
        {
            var hasSettings = false;
            var hasConnectionStrings = false;
            try
            {
                var typeofSettingsClass = context["SettingsClassType"] as Type;
                var props = typeofSettingsClass?.GetProperties();
                if (props != null)
                {
                    for (int i = 0; i < props.Length; i++)
                    {
                        if (props[i].GetCustomAttributes(true).OfType<SpecialSettingAttribute>().Any((specialSettingAttribute) => specialSettingAttribute.SpecialSetting == SpecialSetting.ConnectionString))
                        {
                            hasConnectionStrings = true;
                        }
                        else
                        {
                            hasSettings = true;
                        }
                        if (hasSettings && hasConnectionStrings)
                        {
                            break;
                        }
                    }
                    _hasSettings = hasSettings;
                    _hasConnectionStrings = hasConnectionStrings;
                }
            }
            catch (Exception ex)
            {
                ConfigurationLogging.Error(ex.ToString());
            }
        }

        private void DetectFileSystemOverridesForGroup(VaultGroupConfigurationElement config)
        {
            _overriddenSettingsFileName = (config?.OverrideFileName);
            _overriddenSettings = FileBasedSettingsOverrideHelper.ReadOverriddenSettingsFromFilePath(_overriddenSettingsFileName, ConfigurationLogging.Error);
        }

        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection collection)
        {
            OneTimeInitializeFromContext(context);
            if (_alternateSettings != null)
            {
                return _alternateSettings.GetPropertyValues(context, collection);
            }
            LoadLocalOverrides(collection, out var propertyValues, out var settingsProperties);
            if (_configurationClient == null)
            {
                return propertyValues;
            }
            int maxAttempts = _firstFetch ? _maxRetries : 1;
            if (_hasSettings)
            {
                UpdateSettingsFromVault(settingsProperties, maxAttempts, DateTime.MinValue);
            }
            if (_hasConnectionStrings)
            {
                UpdateConnectionStringsFromVault(settingsProperties, maxAttempts, DateTime.MinValue);
            }
            return propertyValues;
        }

        public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection collection)
        {
            OneTimeInitializeFromContext(context);
            if (_alternateSettings != null)
            {
                _alternateSettings.SetPropertyValues(context, collection);
                return;
            }
            if (_configurationClient == null)
            {
                throw new ConfigurationErrorsException("Data is not defined.");
            }
            foreach (var prop in collection)
            {
                var property = (SettingsPropertyValue)prop;
                if (property.Property.SerializeAs != SettingsSerializeAs.String)
                {
                    ConfigurationLogging.Warning("Property {0}.{1} cannot be saved because it serializes as {2}", _groupName, property.Name, property.Property.SerializeAs);
                }
                else
                {
                    SaveProperty(property);
                }
            }
        }

        private void SaveProperty(SettingsPropertyValue settingsPropertyValue)
        {
            var updated = DateTime.UtcNow.AddHours(-7.0);
            if (IsConnectionString(settingsPropertyValue))
            {
                _lastConnectionStringModDate = updated;
            }
            else
            {
                _lastSettingsModDate = updated;
            }
            try
            {
                _configurationClient.SetProperty(_groupName, settingsPropertyValue.Name, settingsPropertyValue.Property.PropertyType.FullName, (string)settingsPropertyValue.SerializedValue, updated);
            }
            catch (Exception ex)
            {
                ConfigurationLogging.Error(ex.ToString());
            }
        }

        private bool IsConnectionString(SettingsPropertyValue settingsPropertyValue)
        {
            return _hasConnectionStrings && ConfigurationManager.ConnectionStrings[_groupName + "." + settingsPropertyValue.Name] != null;
        }

        private const int _maxRetries = 20;
        private const int _softLogCharacterLimit = 20000;

        private bool _oneTimeInitComplete;
        private string _groupName;
        private bool _hasSettings;
        private bool _hasConnectionStrings;
        private ApplicationSettingsBase _appSettings;
        private SettingsProvider _alternateSettings;
#pragma warning disable IDE0052 // Remove unread private members
        private DateTime _lastSettingsModDate = DateTime.MinValue;
        private DateTime _lastConnectionStringModDate = DateTime.MinValue;
        private bool _firstFetch = true;
        private string _overriddenSettingsFileName;
        private Dictionary<string, object> _overriddenSettings;
        private DateTime _lastFileBasedOverrideModDate = DateTime.MinValue;
        private static readonly VaultProviderConfigurationSection _configurationSection;
        private static readonly VaultConfigurationClient _configurationClient;
        private static readonly string _address;
        private static readonly SelfDisposingTimer _timer;
        private static readonly ConcurrentDictionary<string, VaultProvider> _providers;
#pragma warning restore IDE0052 // Remove unread private members

    }
}
