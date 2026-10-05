namespace Grid.Bot.ClientSettings;

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Collections.Generic;

using VaultSharp;
using VaultSharp.Core;

using Prometheus;

using Logging;

using Internal;

// Simplify these long ass types
using Secrets = System.Collections.Generic.IDictionary<string, object>;
using MetaData = System.Collections.Generic.IDictionary<string, string>;
using CachedValues = Internal.RefreshAhead<System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, object>>>;

/// <summary>
/// Implementation for <see cref="IClientSettingsFactory"/> via Vault.
/// </summary>
/// <seealso cref="IClientSettingsFactory" />
public class ClientSettingsFactory : IClientSettingsFactory
{
    private const string _metadataJsonKeyPrefix = "$$";

    private readonly ClientSettingsSettings _settings;
    private readonly IVaultClient _client;
    private readonly ILogger _logger;
    private readonly LazyWithRetry<CachedValues> _settingsCacheRefreshAhead;

    private static readonly Type[] _supportedTypes = [typeof(string), typeof(bool), typeof(int), typeof(long)];

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

        _settingsCacheRefreshAhead = new LazyWithRetry<CachedValues>(() => CachedValues.ConstructAndPopulate(settings.ClientSettingsRefreshInterval, DoRefreshAsync));
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

                if (!Enum.TryParse<ClientSettingType>(type, true, out var settingType))
                {
                    _logger?.Verbose("Failed to parse setting type '{0}' for setting '{1}'! Defaulting to string.", type, entry.Key);

                    settingType = ClientSettingType.String;
                }

                switch (settingType)
                {
                    case ClientSettingType.String: // bogus, but whatever
                    default:
                        settings.Add(entry.Key, str);

                        break;
                    case ClientSettingType.Bool:
                        if (bool.TryParse(str, out var boolValue))
                            settings.Add(entry.Key, boolValue);
                        else
                            _logger?.Verbose("Failed to parse setting '{0}' as a bool!", entry.Key);

                        break;
                    case ClientSettingType.Int:
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
                case ClientSettingType.Bool when !ClientSettingsNameHelper.IsFilteredSetting(entry.Key):
                    if (bool.TryParse(str, out var boolValue))
                        settings.Add(entry.Key, boolValue);
                    else
                        _logger?.Verbose("Failed to parse setting '{0}' as a bool!", entry.Key);

                    break;
                case ClientSettingType.Int when !ClientSettingsNameHelper.IsFilteredSetting(entry.Key):
                    if (int.TryParse(str, out var intValue))
                        settings.Add(entry.Key, intValue);
                    else
                        _logger?.Verbose("Failed to parse setting '{0}' as an int!", entry.Key);

                    break;
                case ClientSettingType.String:
                default:
                    settings.Add(entry.Key, str);

                    break;
            }
        }

        return settings;
    }

    private async Task<List<(string name, Secrets data, MetaData metadata)>> FetchNewDataAsync()
    {
        var data = new List<(string name, Secrets data, MetaData metadata)>();

        if (_settings.ClientSettingsViaVault)
        {
            _logger?.Debug("Refreshing settings from vault at path '{0}/{1}'", _mount, _path);

            // List all the keys in the path
            var keys = await _client.V1.Secrets.KeyValue.V2.ReadSecretPathsAsync(mountPoint: _mount, path: _path).ConfigureAwait(false);
            if (keys.Data == null || keys.Data.Keys?.Count() == 0)
            {
                _logger?.Debug("No keys found at path '{0}/{1}'", _mount, _path);

                return data;
            }

            // For each key, read the secret
            foreach (var applicationName in keys.Data.Keys)
            {
                var secret = await _client.V1.Secrets.KeyValue.V2.ReadSecretAsync(mountPoint: _mount, path: applicationName).ConfigureAwait(false);
                var metadata = await _client.V1.Secrets.KeyValue.V2.ReadSecretMetadataAsync(mountPoint: _mount, path: applicationName).ConfigureAwait(false);

                data.Add((applicationName, secret.Data.Data, metadata.Data.CustomMetadata));
            }

            return data;
        }

        if (!File.Exists(_settings.ClientSettingsFilePath))
        {
            _logger?.Debug("No file was found at the path '{0}'", _settings.ClientSettingsFilePath);

            return data;
        }

        using var jsonStream = File.OpenRead(_settings.ClientSettingsFilePath);
        var document = JsonDocument.Parse(jsonStream);

        foreach (JsonProperty prop in document.RootElement.EnumerateObject())
        {
            var applicationName = prop.Name;
            var applicationMetatdataName = $"{_metadataJsonKeyPrefix}{applicationName}";

            MetaData metadata = new Dictionary<string, string>();
            if (document.RootElement.TryGetProperty(applicationMetatdataName, out var metadataProp))
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
            _logger?.Debug("Writiting settings to vault at path '{0}/{1}/{2}'",
                           _mount, _path, applicationName);

            _client.V1.Secrets.KeyValue.V2.WriteSecretAsync(
                mountPoint: _mount,
                path: $"{_path}/{applicationName}",
                data: serializedData).Wait();

            _client.V1.Secrets.KeyValue.V2.WriteSecretMetadataAsync(
                mountPoint: _mount, path: $"{_path}/{applicationName}",
                customMetadataRequest: new() { CustomMetadata = metadata.ToDictionary() }).Wait();

            return;
        }

        using var jsonStream = File.Open(_settings.ClientSettingsFilePath, FileMode.OpenOrCreate);
        var document = JsonNode.Parse(jsonStream);

        document[applicationName] = JsonSerializer.Serialize(serializedData);
        document[$"{_metadataJsonKeyPrefix}{applicationName}"] = JsonSerializer.Serialize(metadata);

        using var writer = new Utf8JsonWriter(jsonStream);
        document.WriteTo(writer);

        // Can be made more efficient? can we skip past remote here
        _settingsCacheRefreshAhead.LazyValue.Value[applicationName] = data;
    }

    private async Task<IDictionary<string, Secrets>> DoRefreshAsync(IDictionary<string, Secrets> oldSettings)
    {
        _logger?.Debug("Refreshing settings, FromVault = {0}", _settings.ClientSettingsViaVault);

        var settings = new Dictionary<string, Secrets>();

        try
        {
            // List all the keys in the path
            var data = await FetchNewDataAsync().ConfigureAwait(false);

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

    /// <summary>
    /// Merges the current dictionary with others, left to right.
    /// If a key exists in multiple dictionaries, the value from the last one will be used.
    /// </summary>
    /// <typeparam name="T">The type of the dictionary.</typeparam>
    /// <typeparam name="K">The type of the key.</typeparam>
    /// <typeparam name="V">The type of the value.</typeparam>
    /// <param name="me">The current dictionary.</param>
    /// <param name="others">The dictionaries to merge with.</param>
    /// <returns>A new dictionary containing the merged key-value pairs.</returns>
    public static T MergeDictionaryLeft<T, K, V>(T me, params IDictionary<K, V>[] others)
        where T : IDictionary<K, V>, new()
    {
        var newMap = new T();

        foreach (IDictionary<K, V> src in new List<IDictionary<K, V>> { me }.Concat(others))
        {
            // ^-- echk. Not quite there type-system.
            foreach (KeyValuePair<K, V> p in src)
            {
                newMap[p.Key] = p.Value;
            }
        }

        return newMap;
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
            throw new ArgumentException(string.Format("'{0}' cannot be null or whitespace!", nameof(application)), nameof(application));

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

            if (withDependencies && hasDependencies)
            {
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

                if (dependenciesToMerge.Count > 0)
                {
                    _logger?.Debug("Merging settings for application '{0}' with dependencies: {1}", application, string.Join(", ", dependencyNames));

                    var mergedSettings = new Dictionary<string, object>(settings);

                    settings = MergeDictionaryLeft(mergedSettings, dependenciesToMerge.ToArray());
                }
            }

            return settings;
        }
        finally
        {
            _settingsCacheLock.ExitReadLock();
        }
    }

    /// <inheritdoc cref="IClientSettingsFactory.GetBucketedSettingsForApplication(string, string, bool)"/>
    public Secrets GetBucketedSettingsForApplication(string application, string bucketName, bool withDependencies = true)
    {
        if (string.IsNullOrWhiteSpace(application))
            throw new ArgumentException(string.Format("'{0}' cannot be null or whitespace!", nameof(application)), nameof(application));

        if (string.IsNullOrWhiteSpace(bucketName))
            throw new ArgumentException(string.Format("'{0}' cannot be null or whitespace!", nameof(bucketName)), nameof(bucketName));

        var settings = GetSettingsForApplication(application, withDependencies);

        if (settings == null)
            return null;

        var bucketedSettings = new Dictionary<string, object>();

        foreach (var kvp in settings)
        {
            if (kvp.Key.StartsWith($"{bucketName}_"))
            {
                var keyWithoutBucket = kvp.Key.Substring(bucketName.Length + 1);
                bucketedSettings[keyWithoutBucket] = kvp.Value;
            }
        }

        return bucketedSettings;
    }

    /// <inheritdoc cref="IClientSettingsFactory.GetSettingForApplication{T}(string, string, bool)"/>
    public T GetSettingForApplication<T>(string application, string setting, bool withDependencies = true)
    {
        if (string.IsNullOrWhiteSpace(application))
            throw new ArgumentException(string.Format("'{0}' cannot be null or whitespace!", nameof(application)), nameof(application));

        if (string.IsNullOrWhiteSpace(setting))
            throw new ArgumentException(string.Format("'{0}' cannot be null or whitespace!", nameof(setting)), nameof(setting));

        if (!_supportedTypes.Contains(typeof(T)) || typeof(T) == typeof(ClientSettingsFilteredValue<>))
            throw new ArgumentException(string.Format("'{0}' is not a supported type!", typeof(T).Name), nameof(T));

        var settings = GetSettingsForApplication(application, withDependencies) 
                    ?? throw new InvalidOperationException(string.Format("Application '{0}' not found!", application));

        if (!settings.TryGetValue(setting, out var value))
            throw new InvalidOperationException(string.Format("Setting '{0}' for application '{1}' not found!", setting, application));

        try
        {
            return typeof(T) switch
            {
                Type t when t == typeof(string) => (T)(object)value.ToString(),
                Type t when t == typeof(bool) => (T)value,
                Type t when t == typeof(int) => (T)value,
                _ => throw new ArgumentException(string.Format("'{0}' is not a supported type!", typeof(T).Name)),
            };
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            _logger?.Error(ex);

            throw new InvalidCastException(string.Format(
                "Failed to cast setting '{0}' for application '{1}' to type '{2}'!", 
                setting, 
                application, 
                typeof(T).Name),
                ex
            );
        }
    }

    /// <inheritdoc cref="IClientSettingsFactory.GetFilteredSettingForApplication{T}(string, string, ClientSettingsFilterType, bool)"/>
    public ClientSettingsFilteredValue<T> GetFilteredSettingForApplication<T>(
        string application, 
        string setting, 
        ClientSettingsFilterType filterType = ClientSettingsFilterType.Place, 
        bool withDependencies = false
    ) 
    {
        if (string.IsNullOrWhiteSpace(application))
            throw new ArgumentException(string.Format("'{0}' cannot be null or whitespace!", nameof(application)), nameof(application));

        if (string.IsNullOrWhiteSpace(setting))
            throw new ArgumentException(string.Format("'{0}' cannot be null or whitespace!", nameof(setting)), nameof(setting));

        if (!_supportedTypes.Contains(typeof(T)))
            throw new ArgumentException(string.Format("'{0}' is not a supported type!", typeof(T).Name), nameof(T));

        var settings = GetSettingsForApplication(application, withDependencies) 
                    ?? throw new InvalidOperationException(string.Format("Application '{0}' not found!", application));

        var settingName = $"{setting}_{filterType}Filter";

        if (!settings.TryGetValue(settingName, out var value))
            throw new InvalidOperationException(string.Format("Setting '{0}' for application '{1}' not found!", setting, application));

        try
        {
            return ClientSettingsFilteredValue<T>.FromString(settingName, (string)value);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            _logger?.Error(ex);

            throw new InvalidCastException(string.Format(
                "Failed to cast setting '{0}' for application '{1}' to type '{2}'!", 
                setting, 
                application,
                typeof(T).Name),
                ex
            );
        }
    }

    /// <inheritdoc cref="IClientSettingsFactory.WriteSettingsForApplication(string, Secrets)"/>
    public void WriteSettingsForApplication(string application, Secrets settings)
    {
        if (string.IsNullOrWhiteSpace(application))
            throw new ArgumentException(string.Format("'{0}' cannot be null or whitespace!", nameof(application)), nameof(application));

        ArgumentNullException.ThrowIfNull(settings, nameof(settings));

        var metadata = new Dictionary<string, string>();

        foreach (var kvp in settings)
        {
            if (!_supportedTypes.Contains(kvp.Value.GetType()))
            {
                _logger.Warning("{0}.{1} is not a valid type, skipping!", application, kvp.Key);

                continue;
            }

            if (!ClientSettingsNameHelper.PrefixedSettingRegex().IsMatch(kvp.Key))
                metadata[kvp.Key] = (kvp.Value switch
                {
                    bool => ClientSettingType.Bool,
                    int => ClientSettingType.Int,
                    _ => ClientSettingType.String
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
            throw new ArgumentException(string.Format("'{0}' cannot be null or whitespace!", nameof(application)), nameof(application));

        if (string.IsNullOrWhiteSpace(setting))
            throw new ArgumentException(string.Format("'{0}' cannot be null or whitespace!", nameof(setting)), nameof(setting));

        if (!_supportedTypes.Contains(typeof(T)))
            throw new ArgumentException(string.Format("'{0}' is not a supported type!", typeof(T).Name), nameof(T));

        if (value is null)
            throw new ArgumentNullException(nameof(value));

        var data = GetSettingsForApplication(application, false) ?? new Dictionary<string, object>();
        data[setting] = value;

        _logger?.Debug("Setting '{0}' for application '{1}' to '{2}'", setting, application, value);

        WriteSettingsForApplication(application, data);
    }

    /// <inheritdoc cref="IClientSettingsFactory.SetSettingForApplication(string, string, object, ClientSettingType)"/>
    public void SetSettingForApplication(string application, string setting, object value, ClientSettingType settingType = ClientSettingType.String)
    {
        switch (settingType)
        {
            case ClientSettingType.String:
                SetSettingForApplication(application, setting, value.ToString());
                break;
            case ClientSettingType.Int:
                SetSettingForApplication(application, setting, Convert.ToInt64(value));
                break;
            case ClientSettingType.Bool:
                SetSettingForApplication(application, setting, Convert.ToBoolean(value));
                break;
            default:
                throw new ArgumentException(string.Format("'{0}' is not a supported type!", settingType), nameof(settingType));
        }
    }
}
