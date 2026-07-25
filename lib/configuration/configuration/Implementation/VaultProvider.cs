namespace Configuration;

using System;
using System.Net;
using System.Linq;
using System.Threading;
using System.Text.Json;
using System.Reflection;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.Concurrent;

using VaultSharp;
using VaultSharp.Core;

using Vault;
using Logging;

using Threading;
using Threading.Extensions;

/// <summary>
/// Implementation for <see cref="BaseProvider"/> via Vault.
/// </summary>
public abstract class VaultProvider : EnvironmentProvider, IVaultProvider
{
    private static readonly TimeSpan _defaultRefreshIntervalConstant = TimeSpan.FromMinutes(10);
    private static TimeSpan _defaultRefreshInterval
    {
        get
        {
            if (!TimeSpan.TryParse(Environment.GetEnvironmentVariable("DEFAULT_PROVIDER_REFRESH_INTERVAL"), out var refreshInterval))
                return _defaultRefreshIntervalConstant;

            return refreshInterval;
        }
    }

    private static readonly HashSet<string> _propertyNamesIgnored = new()
    {
        nameof(Mount),
        nameof(Path),
    };

    private static readonly ConcurrentDictionary<string, VaultProvider> _providers = new();

    private bool _disposed = false;

    /// <summary>
    /// The raw cached values.
    /// </summary>
    protected IDictionary<string, object> _CachedValues = new Dictionary<string, object>();

    private static readonly ManualResetEvent _refreshRequestEvent = new(false);
    private static OnceFlag _refreshAheadOnceFlag = new();
    private static readonly IVaultClient _client = VaultClientFactory.Singleton.GetClient();
    private static readonly ILogger _staticLogger = Logger.Singleton;

    /// <inheritdoc cref="IVaultProvider.Mount"/>
    public virtual string Mount { get; set; }

    /// <inheritdoc cref="IVaultProvider.Path"/>
    public virtual string Path { get; set; }

    /// <summary>
    /// Gets the refresh interval for the settings provider.
    /// </summary>
    private static TimeSpan RefreshInterval => _defaultRefreshInterval;

    /// <inheritdoc cref="INotifyPropertyChanged.PropertyChanged"/>
    public override event PropertyChangedEventHandler PropertyChanged;

    private static void TryInitializeGlobalRefreshThread()
    {
        Call.Once(ref _refreshAheadOnceFlag, () => {
            new Thread(RefreshThread)
            {
                IsBackground = true,
                Name = "VaultProvider Refresh Thread"
            }.Start();

            _staticLogger?.Debug("VaultProvider: Started refresh thread!");
        });
    }

    /// <summary>
    /// Refresh all the currently registered providers immediately, this method is asynchronous
    /// and will return immediately.
    /// </summary>
    public static void RefreshAllProviders()
    {
        _refreshRequestEvent.Set();
    }

    /// <inheritdoc cref="BaseProvider.SetRawValue{T}(string, T)"/>
    protected override void SetRawValue<T>(string variable, T value)
    {
        if (_client == null)
        {
            base.SetRawValue(variable, value);

            return;
        }

        _logger?.Information("VaultProvider: Set value in vault at path '{0}/{1}/{2}'", Mount, Path, variable);

        var realValue = ConvertFrom(value, typeof(T));

        _CachedValues[variable] = realValue;

        ApplyCurrent();
    }

    /// <inheritdoc cref="IVaultProvider.ApplyCurrent"/>
    public void ApplyCurrent()
    {
        if (_client == null) return;

        // Build the current from the getters.
        var values = GetLatestValues();

        _logger?.Information("VaultProvider: Writing secret '{0}/{1}' to Vault!", Mount, Path);

        _client?.V1.Secrets.KeyValue.V2.WriteSecretAsync(
            mountPoint: Mount,
            path: Path,
            data: values
        );
    }

    /// <summary>
    /// This boasts the ability to fetch the latest values from the getters.
    /// 
    /// This is because the cache does not cache default values, and we need to fetch the latest values from the getters.
    /// </summary>
    private Dictionary<string, object> GetLatestValues()
    {
        var getters = GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(prop => !_propertyNamesIgnored.Contains(prop.Name));

        var newCachedValues = new Dictionary<string, object>();

        foreach (var getter in getters)
        {
            var getterName = getter.GetCustomAttribute<SettingNameAttribute>()?.Name ?? getter.Name;

            try
            {
                _logger?.Verbose("VaultProvider: Fetching initial value for {0}.{1}", GetType().Name, getterName);

                var value = getter.GetGetMethod().Invoke(this, Array.Empty<object>());
                var realValue = value?.ToString() ?? string.Empty;

                if (value is Array arr)
                    realValue = string.Join(",", arr.Cast<object>().ToArray());

                newCachedValues.Add(getterName, realValue);
            }
            catch (TargetInvocationException ex)
            {
                _logger?.Verbose("VaultProvider: Error occurred when fetching getter for '{0}.{1}': {2}", GetType().Name, getterName, ex.InnerException.Message);

                newCachedValues.Add(getterName, string.Empty);
            }
        }

        return newCachedValues;
    }

    /// <summary>
    /// Construct a new instance of <see cref="VaultProvider"/>
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="periodicRefresh">Should this periodically refresh?</param>
    protected VaultProvider(ILogger logger = null, bool periodicRefresh = true)
    {
        logger ??= Logger.Singleton;

        SetLogger(logger);

        if (periodicRefresh && _client != null) // Periodic refresh only needed in Vault environments
        {
            TryInitializeGlobalRefreshThread();

            _logger?.Debug("VaultProvider: Setup for '{0}/{1}' to refresh every '{2}' interval!", Mount, Path, RefreshInterval);

            if (_providers.TryGetValue(GetType().ToString(), out _))
            {
                _logger?.Debug("VaultProvider: Skipping setup for '{0}/{1}' because it is already setup!", Mount, Path);

                return;
            }

            _providers.TryAdd(GetType().ToString(), this);
        }

        DoRefresh();
    }

    /// <summary>
    /// Construct a new instance of <see cref="VaultProvider"/>
    /// </summary>
    /// <param name="mount">The <see cref="Mount"/></param>
    /// <param name="path">The <see cref="Path"/></param>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="periodicRefresh">Should this periodically refresh?</param>
    protected VaultProvider(string mount, string path = "", ILogger logger = null, bool periodicRefresh = true)
        : this(logger, periodicRefresh)
    {
        Mount = mount;
        Path = path;
    }

    private static void RefreshThread()
    {
        while (true)
        {
            var providers = _providers.ToArray();

            foreach (var kvp in providers)
            {
                try
                {
                    kvp.Value.DoRefresh();
                }
                catch (Exception ex)
                {
                    _staticLogger?.Error(ex);
                }
            }

            _refreshRequestEvent.WaitOne(RefreshInterval); // SetClient makes DoRefresh call.
            _refreshRequestEvent.Reset();
        }
    }

    private void DoRefresh()
    {
        if (_client == null) return;

        _logger?.Debug("VaultProvider: DoRefresh for secret '{0}/{1}'", Mount, Path);

        try
        {
            var secret = _client.V1.Secrets.KeyValue.V2.ReadSecretAsync(
                mountPoint: Mount,
                path: Path
            ).Sync();

            var values = secret.Data.Data;
            InvokePropertyChangedForChangedValues(values);

            lock (_CachedValues)
                _CachedValues = values;
        }
        catch (VaultApiException ex)
        {
            if (ex.HttpStatusCode == HttpStatusCode.NotFound)
            {
                _logger?.Debug("VaultProvider: DoRefresh for secret '{0}/{1}' failed: No secret found!", Mount, Path);

                return;
            }

            _logger?.Error(ex);

            throw;
        }
    }

    private bool HasProperty(ref string name)
    {
        var type = GetType();
        var getter = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);

        if (getter == null)
        {
            var propertyName = name;
            var property = (from prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                            let attrib = prop.GetCustomAttribute<SettingNameAttribute>()
                            where !_propertyNamesIgnored.Contains(prop.Name) && attrib?.Name == propertyName
                            select prop).SingleOrDefault();

            if (property == null)
            {
                _logger?.Debug("VaultProvider: Skipping property changed handler for '{0}' because settings provider '{1}' does not define it!", name, this);

                return false;
            }

            name = property.Name;
        }

        return true;
    }

    private void InvokePropertyChangedForChangedValues(IDictionary<string, object> newValues)
    {
        if (_CachedValues.Count == 0)
        {
            foreach (var kvp in newValues)
            {
                if (_propertyNamesIgnored.Contains(kvp.Key))
                {
                    _logger?.Debug("VaultProvider: Skipping property changed handler for '{0}' as it is a reserved property!", kvp.Key);

                    continue;
                }

                var propertyName = kvp.Key;
                if (!HasProperty(ref propertyName)) continue;

                _logger?.Verbose("VaultProvider: Invoking property changed handler for '{0}'", propertyName);

                PropertyChanged?.Invoke(this, new(propertyName));
            }

            return;
        }

        foreach (var kvp in newValues)
        {
            if (_CachedValues.TryGetValue(kvp.Key, out var value))
                if (value.ToString().Equals(kvp.Value.ToString())) continue;

            if (_propertyNamesIgnored.Contains(kvp.Key))
            {
                _logger?.Debug("VaultProvider: Skipping property changed handler for '{0}' as it is a reserved property!", kvp.Key);

                continue;
            }

            var propertyName = kvp.Key;
            if (!HasProperty(ref propertyName)) continue;

            _logger?.Debug("Invoking property changed handler for '{0}'", propertyName);

            PropertyChanged?.Invoke(this, new(propertyName));
        }
    }

    /// <inheritdoc cref="IVaultProvider.Refresh"/>
    public void Refresh() => DoRefresh();

    /// <inheritdoc cref="BaseProvider.GetRawValue(string, out string)"/>
    protected override bool GetRawValue(string key, out string value)
    {
        if (base.GetRawValue(key, out value)) return true; // ENV is priority;

        object v;

        lock (_CachedValues)
            if (!_CachedValues.TryGetValue(key, out v)) return false;

        if (v is JsonElement element)
            value = element.GetString();
        else
            value = v.ToString();

        return true;
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose()
    {
        if (_disposed) return;

        GC.SuppressFinalize(this);

        _providers.TryRemove(GetType().ToString(), out _);
        _disposed = true;
    }
}
