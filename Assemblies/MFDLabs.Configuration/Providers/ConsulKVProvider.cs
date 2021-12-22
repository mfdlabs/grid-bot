using System;
using System.Collections;
using System.Text;
using System.Linq;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using MFDLabs.Threading;
using MFDLabs.Configuration.Logging;
using MFDLabs.Text.Extensions;
using MFDLabs.Configuration.Sections.ConsulKv;
using MFDLabs.Configuration.Elements.ConsulKv;
using MFDLabs.Configuration.Clients.ConsulKv;

namespace MFDLabs.Configuration.Providers
{
    public class ConsulKvProvider : SettingsProvider
    {
        static ConsulKvProvider()
        {
            ConfigurationLogging.Info("MFDLabs.Configuration.Providers.ConsulKVProvider static init started.");
            ConfigurationSection = ConfigurationManager.GetSection("mfdlabsConsulConfiguration") as ConsulKvProviderConfigurationSection;
            if (ConfigurationSection != null)
            {
                var configuration = GetGroupConfigurationElement();
                var address = configuration.Address;
                ConfigurationClient = new ConsulKvConfigurationClient(address, configuration.AclToken);
                ConfigurationLogging.Info("MFDLabs.Configuration.Providers.ConsulKVProvider static init Consul Client point to address '{0}'.", address);
                var updateInterval = configuration.UpdateInterval;
                Timer = new SelfDisposingTimer(RefreshRegisteredProviders, updateInterval, updateInterval);
                Providers = new ConcurrentDictionary<string, ConsulKvProvider>();
                ConfigurationLogging.Info("MFDLabs.Configuration.Providers.ConsulKVProvider static init Timer created, refresh every '{0}'.", updateInterval);
                return;
            }
            ConfigurationLogging.Warning("No config file found with mfdlabsConsulConfiguration.");
        }

        public static void Register(SettingsLoadedEventArgs e, ApplicationSettingsBase settings)
        {
            var provider = e.Provider as ConsulKvProvider;
            provider?.UpdateApplicationSettings(settings);
            var groupName = provider?._groupName;
            if (groupName.IsNullOrEmpty() || Providers == null ||
                Providers.ContainsKey(groupName ?? string.Empty)) return;
            
            if (Providers.TryAdd(groupName, provider))
            {
                ConfigurationLogging.Info("Settings Group '{0}' is registered.", groupName);
                return;
            }
            ConfigurationLogging.Info("Settings Group '{0}' failed to register.", groupName);
        }

        private static void RefreshRegisteredProviders()
        {
            try
            {
                ConfigurationLogging.Info("RefreshRegisteredProviders - Start on {0} settings providers.", Providers.Count);
                Timer.Pause();
                foreach (var provider in Providers)
                {
                    provider.Value.ReloadChangedSettings();
                }
                ConfigurationLogging.Info("RefreshRegisteredProviders - Complete on {0} settings providers.", Providers.Count);
            }
            catch (Exception ex)
            {
                ConfigurationLogging.Error("RefreshRegisteredProviders in MFDLabs.Configuration.Providers.ConsulKVProvider failed with the following\n {0}.", ex);
            }
            finally
            {
                Timer.Unpause();
            }
        }

        public override void Initialize(string name, NameValueCollection col)
        {
            if (string.IsNullOrEmpty(name)) name = GetType().FullName;
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
                        _overriddenSettings = FileBasedSettingsOverrideHelper.ReadOverriddenSettingsFromFilePath(_overriddenSettingsFileName);
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

        public override string Description => "A consul-backed SettingsProvider that synchronizes assembly settings from a common consul kv repository.";

        public override string ApplicationName { get; set; }

        private static ConsulKvGroupConfigurationElement GetGroupConfigurationElement(string groupName = "*")
        {
            if (ConfigurationSection != null) 
                return ConfigurationSection.Groups[groupName] ?? ConfigurationSection.Groups["*"];
            return null;
        }

        private void LoadLocalOverrides(SettingsPropertyCollection collection, out SettingsPropertyValueCollection settingsPropertyValueCollection, out Dictionary<string, SettingsPropertyValue> settingsProperties)
        {
            settingsPropertyValueCollection = new SettingsPropertyValueCollection();
            settingsProperties = new Dictionary<string, SettingsPropertyValue>(collection.Count);
            foreach (var obj in collection)
            {
                var settingsProperty = (SettingsProperty)obj;
                var value = new SettingsPropertyValue(settingsProperty);
                var name = $"{_groupName}.{settingsProperty.Name}";
                if (TryGetOverriddenSettingValue(name, out var propertyValue)) 
                    value.PropertyValue = propertyValue;
                settingsPropertyValueCollection.Add(value);
                settingsProperties[settingsProperty.Name] = value;
            }
        }

        private void UpdateConnectionStringsFromConsulKv(IReadOnlyDictionary<string, SettingsPropertyValue> settingsProperties, int maxAttempts, DateTime lastModification)
        {
            var allConnectionStrings = ConsulKvConfigurationClient.FetchWithRetries(ConfigurationClient.GetAllConnectionStrings, _groupName, maxAttempts);
            _firstFetch = false;
            var connectionStrings = new List<string>();
            foreach (var connectionString in allConnectionStrings)
            {
                if (connectionString.Updated > lastModification) lastModification = connectionString.Updated;
                if (!settingsProperties.TryGetValue(connectionString.Name, out var property)) 
                    connectionStrings.Add(connectionString.Name);
                else
                    property.SerializedValue = connectionString.Value;
            }
            if (connectionStrings.Count > 0) 
                ConfigurationLogging.Warning(BuildUnknownSettingsMessage(connectionStrings, "Connection Strings"));
        }

        private void UpdateSettingsFromConsulKv(IReadOnlyDictionary<string, SettingsPropertyValue> settingsProperties, int maxAttempts, DateTime lastModification)
        {
            var allSettings = ConsulKvConfigurationClient.FetchWithRetries(ConfigurationClient.GetAllSettings, _groupName, maxAttempts);
            _firstFetch = false;
            var settings = new List<string>();
            foreach (var setting in allSettings)
            {
                if (setting.Updated > lastModification) lastModification = setting.Updated;
                if (!settingsProperties.TryGetValue(setting.Name, out var property)) 
                    settings.Add(setting.Name);
                else
                    property.SerializedValue = TryGetOverriddenSettingValue($"{_groupName}.{setting.Name}", out var value) ? value : setting.Value;
            }
            if (settings.Count > 0) 
                ConfigurationLogging.Warning(BuildUnknownSettingsMessage(settings, "Settings"));
        }

        private string BuildUnknownSettingsMessage(List<string> unknownSettings, string settingType)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"The following unknown {settingType} are defined in group: {_groupName}");
            builder.AppendLine();
            var count = 0;
            foreach (var setting in unknownSettings)
            {
                if (builder.Length >= SoftLogCharacterLimit)
                {
                    builder.Append($" and {unknownSettings.Count - count} others. Message Truncated.");
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
            if (_overriddenSettings == null || !_overriddenSettings.TryGetValue(settingName, out var result))
                return false;
            overriddenValue = result;
            return true;
        }

        private void OneTimeInitializeFromContext(SettingsContext context)
        {
            ConfigurationLogging.Info("OneTimeInitializeFromContext from {0}", context);
            if (_oneTimeInitComplete)  return;
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
            if (string.IsNullOrEmpty(el?.Address)) DetectAlternateSettings();
            _oneTimeInitComplete = true;
        }

        private void DetectAlternateSettings()
        {
            ConfigurationLogging.Info("Group {0} is using file-based configuration", _groupName);
            _alternateSettings = new LocalFileSettingsProvider();
            _alternateSettings.Initialize(null, null);
        }

        private void DetectSettingsAndConnectionStringInClass(IDictionary context)
        {
            var hasSettings = false;
            var hasConnectionStrings = false;
            try
            {
                var typeofSettingsClass = context["SettingsClassType"] as Type;
                var props = typeofSettingsClass?.GetProperties();
                if (props == null) return;
                
                foreach (var t in props)
                {
                    if (t.GetCustomAttributes(true)
                        .OfType<SpecialSettingAttribute>()
                        .Any(specialSettingAttribute =>
                            specialSettingAttribute.SpecialSetting == SpecialSetting.ConnectionString))
                        hasConnectionStrings = true;
                    else
                        hasSettings = true;
                    if (hasSettings && hasConnectionStrings) break;
                }
                _hasSettings = hasSettings;
                _hasConnectionStrings = hasConnectionStrings;
            }
            catch (Exception ex)
            {
                ConfigurationLogging.Error(ex.ToString());
            }
        }

        private void DetectFileSystemOverridesForGroup(ConsulKvGroupConfigurationElement config)
        {
            _overriddenSettingsFileName = config?.OverrideFileName;
            _overriddenSettings = FileBasedSettingsOverrideHelper.ReadOverriddenSettingsFromFilePath(_overriddenSettingsFileName, ConfigurationLogging.Error);
        }

        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection collection)
        {
            OneTimeInitializeFromContext(context);
            if (_alternateSettings != null) return _alternateSettings.GetPropertyValues(context, collection);
            LoadLocalOverrides(collection, out var propertyValues, out var settingsProperties);
            if (ConfigurationClient == null) return propertyValues;
            var maxAttempts = _firstFetch ? MaxRetries : 1;
            if (_hasSettings) UpdateSettingsFromConsulKv(settingsProperties, maxAttempts, DateTime.MinValue);
            if (_hasConnectionStrings) UpdateConnectionStringsFromConsulKv(settingsProperties, maxAttempts, DateTime.MinValue);
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
            if (ConfigurationClient == null)
            {
                throw new ConfigurationErrorsException("Data is not defined.");
            }
            foreach (var prop in collection)
            {
                var property = (SettingsPropertyValue)prop;
                if (property.Property.SerializeAs != SettingsSerializeAs.String)
                {
                    ConfigurationLogging.Warning("Property {0}.{1} cannot be saved because it serializes as {2}",
                _groupName,
                        property.Name,
                        property.Property.SerializeAs);
                }
                else
                {
                    SaveProperty(property);
                }
            }
        }

        private void SaveProperty(SettingsPropertyValue settingsPropertyValue)
        {
            var updated = DateTime.UtcNow.AddHours(-7);
            if (IsConnectionString(settingsPropertyValue))
            {
            }

            try
            {
                ConfigurationClient.SetProperty(_groupName,
                    settingsPropertyValue.Name,
                    settingsPropertyValue.Property.PropertyType.FullName,
                    (string) settingsPropertyValue.SerializedValue,
                    updated);
            }
            catch (Exception ex)
            {
                ConfigurationLogging.Error(ex.ToString());
            }
        }

        private bool IsConnectionString(SettingsPropertyValue settingsPropertyValue) => _hasConnectionStrings && ConfigurationManager.ConnectionStrings[_groupName + "." + settingsPropertyValue.Name] != null;

        private const int MaxRetries = 20;
        private const int SoftLogCharacterLimit = 20000;

        private bool _oneTimeInitComplete;
        private string _groupName;
        private bool _hasSettings;
        private bool _hasConnectionStrings;
        private ApplicationSettingsBase _appSettings;
        private SettingsProvider _alternateSettings;
        private bool _firstFetch = true;
        private string _overriddenSettingsFileName;
        private Dictionary<string, object> _overriddenSettings;
        private DateTime _lastFileBasedOverrideModDate = DateTime.MinValue;
        private static readonly ConsulKvProviderConfigurationSection ConfigurationSection;
        private static readonly ConsulKvConfigurationClient ConfigurationClient;
        private static readonly SelfDisposingTimer Timer;
        private static readonly ConcurrentDictionary<string, ConsulKvProvider> Providers;

    }
}
