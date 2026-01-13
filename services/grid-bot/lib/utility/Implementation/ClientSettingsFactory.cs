namespace Grid.Bot.Utility;

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Collections.Generic;

using VaultSharp;
using VaultSharp.Core;

using Prometheus;

using Logging;
using Threading.Extensions;

using Grid.Bot.Extensions;

// Simplify these long ass types
using Secrets = System.Collections.Generic.IDictionary<string, object>;
using MetaData = System.Collections.Generic.IDictionary<string, string>;
using CachedValues = RefreshAhead<System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, object>>>;

/// <summary>
/// Implementation for <see cref="IClientSettingsFactory"/> via Vault.
/// </summary>
/// <seealso cref="IClientSettingsFactory" />
public class ClientSettingsFactory : IClientSettingsFactory
{
    private const string MetadataJsonKeyPrefix = "$$";

    private readonly ClientSettingsSettings _settings;
    private readonly IVaultClient _client;
    private readonly ILogger _logger;
    private readonly LazyWithRetry<CachedValues> _settingsCacheRefreshAhead;

    private static readonly Type[] SupportedTypes = [typeof(string), typeof(bool), typeof(int), typeof(long)];

    private readonly string _mount;
    private readonly string _path;

    private readonly Counter _settingsRefreshCounter = Metrics.CreateCounter(
        "rbx_client_settings_refresh_total",
        "Number of times the client settings have been refreshed.",
        "application"
    );
    private readonly Counter _settingsWriteCounter = Metrics.CreateCounter(
        "rbx_client_settings_write_total",
        "Number of times the client settings have been written.",
        "application"
    );
    private readonly Counter _settingsReadCounter = Metrics.CreateCounter(
        "rbx_client_settings_read_total",
        "Number of times the client settings have been read.",
        "application"
    );

    private readonly ReaderWriterLockSlim _settingsCacheLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientSettingsFactory"/>
    /// class.
    /// </summary>
    /// <param name="client">The <see cref="IVaultClient"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="settings">The <see cref="ClientSettingsSettings"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="client"/> is <see langword="null"/>, only when <see cref="ClientSettingsSettings.ClientSettingsViaVault"/> is <see langword="true"/>.
    /// - <paramref name="logger"/> is <see langword="null"/>.
    /// - <paramref name="settings"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException"><see cref="ClientSettingsSettings.ClientSettingsVaultMount"/> is <see
    /// langword="null"/> or whitespace.</exception> <exception
    /// cref="ArgumentOutOfRangeException"><see cref="ClientSettingsSettings.ClientSettingsRefreshInterval"/> is
    /// less than <see cref="TimeSpan.Zero"/>.</exception>
    public ClientSettingsFactory(
      IVaultClient client,
      ILogger logger,
      ClientSettingsSettings settings
    )
    {
        _client = client;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        if (_settings.ClientSettingsViaVault && _client == null)
            throw new ArgumentNullException(nameof(client), "Client cannot be null when using Vault.");

        if (string.IsNullOrWhiteSpace(settings.ClientSettingsVaultMount))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(settings.ClientSettingsVaultMount));

        if (settings.ClientSettingsRefreshInterval < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(settings.ClientSettingsRefreshInterval), settings.ClientSettingsRefreshInterval, "Value cannot be less than zero.");

        _mount = settings.ClientSettingsVaultMount;
        _path = settings.ClientSettingsVaultPath ?? "/";

        _settingsCacheRefreshAhead = new LazyWithRetry<CachedValues>(() => CachedValues.ConstructAndPopulate(settings.ClientSettingsRefreshInterval, DoRefresh));
    }

    private Dictionary<string, object> ParseSecrets(Secrets secrets, MetaData metadata)
    {
        // rbx-client-settings are like this:
        // FFlag is a bool
        // FInt is an int
        // FString is a string
        //
        // Settings prefixed with no prefix (such as FFlagTest) are static settings.
        // Settings prefixed with D (such as DFFlagTest) are dynamic settings.
        // Settings prefixed with S (such as SFFlagTest) are server synchronized
        // settings.
        //
        // Anything that does not follow these prefixes is parsed as a string,
        // unless there is a metadata key with the name of the setting that
        // corresponds to a type that is not a string, in which case it is parsed as
        // that type.

        var settings = new Dictionary<string, object>();

        foreach (var entry in secrets)
        {
            if (entry.Value is not JsonElement el)
            {
                _logger?.Verbose("Skipping setting '{0}' because it is not a string!", entry.Key);

                continue;
            }

            var str = el.GetString();

            if (!ClientSettingsNameHelper.PrefixedSettingRegex().IsMatch(entry.Key))
            {
                // Setting has no prefix so check metadata for type, 
                // if not present just return as-is
                if (metadata == null || !metadata.TryGetValue(entry.Key, out var type))
                {
                    _logger?.Verbose("Skipping setting '{0}' because it is not prefixed and has no metadata!", entry.Key);

                    settings.Add(entry.Key, str);

                    continue;
                }

                if (!Enum.TryParse<SettingType>(type, true, out var settingType))
                {
                    _logger?.Verbose("Failed to parse setting type '{0}' for setting '{1}'! Defaulting to string.", type, entry.Key);

                    settingType = SettingType.String;
                }

                switch (settingType)
                {
                    case SettingType.String: // bogus, but whatever
                    default:
                        settings.Add(entry.Key, str);

                        break;
                    case SettingType.Bool:
                        if (bool.TryParse(str, out var boolValue))
                            settings.Add(entry.Key, boolValue);
                        else
                            _logger?.Verbose("Failed to parse setting '{0}' as a bool!", entry.Key);

                        break;
                    case SettingType.Int:
                        if (int.TryParse(str, out var intValue))
                            settings.Add(entry.Key, intValue);
                        else
                            _logger?.Verbose("Failed to parse setting '{0}' as an int!", entry.Key);

                        break;
                }

                continue;
            }

            var sType = ClientSettingsNameHelper.GetSettingTypeFromName(entry.Key);
            switch (sType)
            {
                case SettingType.Bool when !ClientSettingsNameHelper.IsFilteredSetting(entry.Key):
                    if (bool.TryParse(str, out var boolValue))
                        settings.Add(entry.Key, boolValue);
                    else
                        _logger?.Verbose("Failed to parse setting '{0}' as a bool!", entry.Key);

                    break;
                case SettingType.Int when !ClientSettingsNameHelper.IsFilteredSetting(entry.Key):
                    if (int.TryParse(str, out var intValue))
                        settings.Add(entry.Key, intValue);
                    else
                        _logger?.Verbose("Failed to parse setting '{0}' as an int!", entry.Key);

                    break;
                case SettingType.String:
                default:
                    settings.Add(entry.Key, str);

                    break;
            }
        }

        return settings;
    }

    private List<(string name, Secrets data, MetaData metadata)> FetchNewData()
    {
        var data = new List<(string name, Secrets data, MetaData metadata)>();

        if (_settings.ClientSettingsViaVault)
        {
            _logger?.Debug("Refreshing settings from vault at path '{0}/{1}'", _mount, _path);

            // List all the keys in the path
            var keys = _client.V1.Secrets.KeyValue.V2.ReadSecretPathsAsync(mountPoint: _mount, path: _path).Sync();
            if (keys.Data == null || keys.Data.Keys?.Count() == 0)
            {
                _logger?.Debug("No keys found at path '{0}/{1}'", _mount, _path);

                return data;
            }

            // For each key, read the secret
            data.AddRange((
                from applicationName in 
                    keys.Data.Keys 
                let secret = _client.V1.Secrets.KeyValue.V2.ReadSecretAsync(mountPoint: _mount, path: applicationName).Sync() 
                let metadata = _client.V1.Secrets.KeyValue.V2.ReadSecretMetadataAsync(mountPoint: _mount, path: applicationName).Sync() 
                select (applicationName, secret.Data.Data, metadata.Data.CustomMetadata))
                    .Select(dummy => ((string name, Secrets data, MetaData metadata))dummy));

            return data;
        }

        if (!File.Exists(_settings.ClientSettingsFilePath))
        {
            _logger?.Debug("No file was found at the path '{0}'", _settings.ClientSettingsFilePath);

            return data;
        }

        using var jsonStream = File.OpenRead(_settings.ClientSettingsFilePath);
        var document = JsonDocument.Parse(jsonStream);

        foreach (var prop in document.RootElement.EnumerateObject())
        {
            var applicationName = prop.Name;
            var applicationMetadataName = $"{MetadataJsonKeyPrefix}{applicationName}";

            MetaData metadata = new Dictionary<string, string>();
            if (document.RootElement.TryGetProperty(applicationMetadataName, out var metadataProp))
                metadata = metadataProp.Deserialize<MetaData>();

            var applicationData = prop.Value.Deserialize<Secrets>();

            data.Add((applicationName, applicationData, metadata));
        }

        return data;
    }

    private void DoCommit(string applicationName, Secrets data, MetaData metadata)
    {
        var serializedData = data.ToDictionary(k => k.Key, v => v.Value.ToString());
        _settingsWriteCounter.WithLabels(applicationName).Inc();

        if (_settings.ClientSettingsViaVault)
        {
            _logger?.Debug("Writing settings to vault at path '{0}/{1}/{2}'",
                           _mount, _path, applicationName);

            _client.V1.Secrets.KeyValue.V2 .WriteSecretAsync(
                mountPoint: _mount,
                path: $"{_path}/${applicationName}",
                data: serializedData).Wait();

            _client.V1.Secrets.KeyValue.V2.WriteSecretMetadataAsync(
                mountPoint: _mount, path: $"{_path}/{applicationName}",
                customMetadataRequest: new() { CustomMetadata = metadata.ToDictionary() }).Wait();

            return;
        }

        using var jsonStream = File.Open(_settings.ClientSettingsFilePath, FileMode.OpenOrCreate);
        var document = JsonNode.Parse(jsonStream);

        if (document != null)
        {
            document[applicationName] = JsonSerializer.Serialize(serializedData);
            document[$"{MetadataJsonKeyPrefix}{applicationName}"] = JsonSerializer.Serialize(metadata);

            using var writer = new Utf8JsonWriter(jsonStream);
            document.WriteTo(writer);
        }

        // Can be made more efficient? can we skip past remote here
        _settingsCacheRefreshAhead.LazyValue.Value[applicationName] = data;
    }

    private IDictionary<string, Secrets> DoRefresh(IDictionary<string, Secrets> oldSettings)
    {
        _logger?.Debug("Refreshing settings, FromVault = {0}", _settings.ClientSettingsViaVault);

        var settings = new Dictionary<string, Secrets>();

        try
        {
            // List all the keys in the path
            var data = FetchNewData();

            // For each key, read the secret
            foreach (var (applicationName, applicationData, applicationMetaData) in data)
            {
                _settingsRefreshCounter.WithLabels(applicationName).Inc();

                var parsedSecrets = ParseSecrets(applicationData, applicationMetaData);
                settings.Add(applicationName, parsedSecrets);
            }

            return settings;
        }
        catch (VaultApiException ex)
        {
            _logger?.Error(ex);

            return settings;
        }
    }

    /// <inheritdoc cref="IClientSettingsFactory.RawSettings"/>
    public IDictionary<string, Secrets> RawSettings
    {
        get
        {
            _settingsCacheLock.EnterReadLock();

            _settingsReadCounter.WithLabels(_mount, _path).Inc();

            try
            {
                return _settingsCacheRefreshAhead.LazyValue.Value;
            }
            finally
            {
                _settingsCacheLock.ExitReadLock();
            }
        }
    }

    /// <inheritdoc cref="IClientSettingsFactory.Refresh"/>
    public void Refresh()
    {
        _settingsCacheRefreshAhead.LazyValue.Refresh();
    }

    /// <inheritdoc cref="IClientSettingsFactory.GetSettingsForApplication(string, bool)"/>
    public Secrets GetSettingsForApplication(string application, bool withDependencies = true)
    {
        if (string.IsNullOrWhiteSpace(application))
            throw new ArgumentException($"'{nameof(application)}' cannot be null or whitespace!", nameof(application));

        _settingsCacheLock.EnterReadLock();

        _settingsReadCounter.WithLabels(application).Inc();

        try
        {
            var hasDependencies = _settings.ClientSettingsApplicationDependencies.TryGetValue(application, out var dependencies);

            if (!_settingsCacheRefreshAhead.LazyValue.Value.TryGetValue(application, out var settings) && !hasDependencies)
            {
                _logger?.Debug("Settings for application '{0}' not found!", application);

                return null;
            }

            settings ??= new Dictionary<string, object>();

            if (!withDependencies || !hasDependencies) return settings;
            
            var dependenciesToMerge = new List<Secrets>();
            var dependencyNames = dependencies.Split(',');

            foreach (var dependency in dependencyNames)
            {
                if (!_settingsCacheRefreshAhead.LazyValue.Value.TryGetValue(dependency, out var dependencySettings))
                {
                    _logger?.Debug("Dependency '{0}' for application '{1}' not found!", dependency, application);

                    continue;
                }

                _logger?.Debug("Dependency '{0}' for application '{1}' found!", dependency, application);
                _settingsReadCounter.WithLabels(dependency).Inc();

                dependenciesToMerge.Add(dependencySettings);
            }

            if (dependenciesToMerge.Count <= 0) return settings;
            
            _logger?.Debug("Merging settings for application '{0}' with dependencies: {1}", application, string.Join(", ", dependencyNames));

            var mergedSettings = new Dictionary<string, object>(settings);

            settings = mergedSettings.MergeLeft(dependenciesToMerge.ToArray());

            return settings;
        }
        finally
        {
            _settingsCacheLock.ExitReadLock();
        }
    }

    /// <inheritdoc cref="IClientSettingsFactory.GetSettingForApplication{T}(string, string, bool)"/>
    public T GetSettingForApplication<T>(string application, string setting, bool withDependencies = true)
    {
        if (string.IsNullOrWhiteSpace(application))
            throw new ArgumentException($"'{nameof(application)}' cannot be null or whitespace!", nameof(application));

        if (string.IsNullOrWhiteSpace(setting))
            throw new ArgumentException($"'{nameof(setting)}' cannot be null or whitespace!", nameof(setting));

        if (!SupportedTypes.Contains(typeof(T)) || typeof(T) == typeof(FilteredValue<>))
            throw new ArgumentException($"'{typeof(T).Name}' is not a supported type!", nameof(T));

        var settings = GetSettingsForApplication(application, withDependencies) 
                    ?? throw new InvalidOperationException($"Application '{application}' not found!");

        if (!settings.TryGetValue(setting, out var value))
            throw new InvalidOperationException($"Setting '{setting}' for application '{application}' not found!");

        try
        {
            return typeof(T) switch
            {
                { } t when t == typeof(string) => (T)(object)value.ToString(),
                { } t when t == typeof(bool) => (T)value,
                { } t when t == typeof(int) => (T)value,
                _ => throw new ArgumentException($"'{typeof(T).Name}' is not a supported type!"),
            };
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            _logger?.Error(ex);

            throw new InvalidCastException(
                $"Failed to cast setting '{setting}' for application '{application}' to type '{typeof(T).Name}'!",
                ex
            );
        }
    }

    /// <inheritdoc cref="IClientSettingsFactory.GetFilteredSettingForApplication{T}(string, string, FilterType, bool)"/>
    public FilteredValue<T> GetFilteredSettingForApplication<T>(
        string application, 
        string setting, 
        FilterType filterType = FilterType.Place, 
        bool withDependencies = false
    ) 
    {
        if (string.IsNullOrWhiteSpace(application))
            throw new ArgumentException($"'{nameof(application)}' cannot be null or whitespace!", nameof(application));

        if (string.IsNullOrWhiteSpace(setting))
            throw new ArgumentException($"'{nameof(setting)}' cannot be null or whitespace!", nameof(setting));

        if (!SupportedTypes.Contains(typeof(T)))
            throw new ArgumentException($"'{typeof(T).Name}' is not a supported type!", nameof(T));

        var settings = GetSettingsForApplication(application, withDependencies) 
                    ?? throw new InvalidOperationException($"Application '{application}' not found!");

        var settingName = $"{setting}_{filterType}Filter";

        if (!settings.TryGetValue(settingName, out var value))
            throw new InvalidOperationException($"Setting '{setting}' for application '{application}' not found!");

        try
        {
            return FilteredValue<T>.FromString(settingName, (string)value);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            _logger?.Error(ex);

            throw new InvalidCastException(
                $"Failed to cast setting '{setting}' for application '{application}' to type '{typeof(T).Name}'!",
                ex
            );
        }
    }

    /// <inheritdoc cref="IClientSettingsFactory.WriteSettingsForApplication(string, Secrets)"/>
    public void WriteSettingsForApplication(string application, Secrets settings)
    {
        if (string.IsNullOrWhiteSpace(application))
            throw new ArgumentException($"'{nameof(application)}' cannot be null or whitespace!", nameof(application));

        ArgumentNullException.ThrowIfNull(settings);

        var metadata = new Dictionary<string, string>();

        foreach (var kvp in settings)
        {
            if (!SupportedTypes.Contains(kvp.Value.GetType()))
            {
                _logger.Warning("{0}.{1} is not a valid type, skipping!", application, kvp.Key);

                continue;
            }

            if (!ClientSettingsNameHelper.PrefixedSettingRegex().IsMatch(kvp.Key))
                metadata[kvp.Key] = (kvp.Value switch
                {
                    bool => SettingType.Bool,
                    int => SettingType.Int,
                    _ => SettingType.String
                }).ToString();
        }

        _settingsCacheLock.EnterWriteLock();

        _settingsCacheRefreshAhead.LazyValue.Value.TryAdd(application, settings);

        try
        {
            DoCommit(application, settings, metadata);
        }
        catch (Exception ex)
        {
            _logger.Error(ex);

            throw;
        }
        finally
        {
            _settingsCacheLock.ExitWriteLock();
        }
    }

    /// <inheritdoc cref="IClientSettingsFactory.SetSettingForApplication{T}(string, string, T)"/>
    public void SetSettingForApplication<T>(string application, string setting, T value)
    {
        if (string.IsNullOrWhiteSpace(application))
            throw new ArgumentException($"'{nameof(application)}' cannot be null or whitespace!", nameof(application));

        if (string.IsNullOrWhiteSpace(setting))
            throw new ArgumentException($"'{nameof(setting)}' cannot be null or whitespace!", nameof(setting));

        if (!SupportedTypes.Contains(typeof(T)))
            throw new ArgumentException($"'{typeof(T).Name}' is not a supported type!", nameof(T));

        if (value is null)
            throw new ArgumentNullException(nameof(value));

        var data = GetSettingsForApplication(application, false) ?? new Dictionary<string, object>();
        data[setting] = value;

        _logger?.Debug("Setting '{0}' for application '{1}' to '{2}'", setting, application, value);

        WriteSettingsForApplication(application, data);
    }

    /// <inheritdoc cref="IClientSettingsFactory.SetSettingForApplication(string, string, object, SettingType)"/>
    public void SetSettingForApplication(string application, string setting, object value, SettingType settingType = SettingType.String)
    {
        switch (settingType)
        {
            case SettingType.String:
                SetSettingForApplication(application, setting, value.ToString());
                break;
            case SettingType.Int:
                SetSettingForApplication(application, setting, Convert.ToInt64(value));
                break;
            case SettingType.Bool:
                SetSettingForApplication(application, setting, Convert.ToBoolean(value));
                break;
            default:
                throw new ArgumentException($"'{settingType}' is not a supported type!", nameof(settingType));
        }
    }
}
