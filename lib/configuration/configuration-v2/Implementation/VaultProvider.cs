namespace Configuration;

using System;
using System.Net;
using System.Linq;
using System.Threading;
using System.Text.Json;
using System.Reflection;
using System.ComponentModel;
using System.Collections.Generic;

using VaultSharp;
using VaultSharp.Core;

using Logging;
using Threading.Extensions;

/// <summary>
/// Implementation for <see cref="BaseProvider"/> via Vault.
/// </summary>
public abstract class VaultProvider : BaseProvider, IVaultProvider
{
    private static readonly string[] _propertyNamesIgnoredForCacheInit = new[]
    {
        nameof(Mount),
        nameof(Path)
    };

    private static readonly TimeSpan _defaultRefreshInterval = TimeSpan.FromMinutes(10);

    private IDictionary<string, object> _cachedValues = new Dictionary<string, object>();
    private Thread _refreshThread;
    private IVaultClient _client;

    private readonly object _clientLock = new();
    private readonly TimeSpan _refreshInterval;

    /// <inheritdoc cref="IVaultProvider.Mount"/>
    public abstract string Mount { get; }

    /// <inheritdoc cref="IVaultProvider.Path"/>
    public abstract string Path { get; }

    /// <inheritdoc cref="INotifyPropertyChanged.PropertyChanged"/>
    public override event PropertyChangedEventHandler PropertyChanged;

    /// <inheritdoc cref="BaseProvider.Set{T}(string, T)"/>
    public override void Set<T>(string variable, T value)
    {
        if (_client == null) return;

        _logger?.Debug("Set value in vault at path '{0}/{1}/{2}'", Mount, Path, variable);

        var realValue = value.ToString();

        if (typeof(T).IsArray)
            realValue = string.Join(",", (value as Array).Cast<object>().ToArray());

        _cachedValues[variable] = realValue;

        PropertyChanged?.Invoke(this, new(variable));

        ApplyCurrent();
    }

    /// <inheritdoc cref="IVaultProvider.ApplyCurrent"/>
    public void ApplyCurrent()
    {
        if (_client == null) return;

        _logger?.Debug("Writing secret '{0}/{1}' to Vault!", Mount, Path);

        _client?.V1.Secrets.KeyValue.V2.WriteSecretAsync(
            mountPoint: Mount,
            path: Path,
            data: _cachedValues
        );
    }

    /// <summary>
    /// Construct a new instance of <see cref="VaultProvider"/>
    /// </summary>
    /// <param name="refreshInterval">The refresh interval.</param>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="client">The <see cref="IVaultClient"/></param>
    protected VaultProvider(
        TimeSpan? refreshInterval = null,
        ILogger logger = null,
        IVaultClient client = null
    )
    {
        if (logger != null)
            SetLogger(logger);

        ApplyInitialCache();

        _refreshInterval = refreshInterval ?? _defaultRefreshInterval;

        if (client != null)
            SetClient(client);
    }

    private void RefreshThread()
    {
        while (true)
        {
            Thread.Sleep(_refreshInterval); // SetClient makes DoRefresh call.

            DoRefresh();
        }
    }

    private void ApplyInitialCache()
    {
        var getters = GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(prop => !_propertyNamesIgnoredForCacheInit.Contains(prop.Name));

        var newCachedValues = new Dictionary<string, object>();

        foreach (var getter in getters)
        {
            try
            {
                _logger?.Debug("Fetching initial value for {0}.{1}", GetType().Name, getter.Name);

                var value = getter.GetGetMethod().Invoke(this, Array.Empty<object>());
                var realValue = value?.ToString() ?? string.Empty;

                if (value is Array arr)
                    realValue = string.Join(",", arr.Cast<object>().ToArray());

                newCachedValues.Add(getter.Name, realValue);
            }
            catch (TargetInvocationException ex)
            {
                _logger?.Warning("Error occurred when fetching getter for '{0}.{1}': {2}", GetType().Name, getter.Name, ex.InnerException.Message);

                newCachedValues.Add(getter.Name, string.Empty);
            }
        }

        _cachedValues = newCachedValues;
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

            _cachedValues = values;
        }
        catch (VaultApiException ex)
        {
            if (ex.HttpStatusCode == HttpStatusCode.NotFound) return;

            _logger?.Error(ex);

            throw;
        }
    }

    private void InvokePropertyChangedForChangedValues(IDictionary<string, object> newValues)
    {
        if (_cachedValues.Count == 0)
        {
            foreach (var kvp in newValues)
            {
                _logger?.Debug("Invoking property changed handler for '{0}'", kvp.Key);

                PropertyChanged?.Invoke(this, new(kvp.Key));
            }

            return;
        }

        foreach (var kvp in newValues)
        {
            if (_cachedValues.TryGetValue(kvp.Key, out var value))
                if (value.Equals(kvp.Value)) continue;

            _logger?.Debug("Invoking property changed handler for '{0}'", kvp.Key);

            PropertyChanged?.Invoke(this, new(kvp.Key));
        }
    }

    /// <inheritdoc cref="IVaultProvider.Refresh"/>
    public void Refresh() => DoRefresh();

    /// <inheritdoc cref="IVaultProvider.SetClient(IVaultClient, bool)"/>
    public void SetClient(IVaultClient client = null, bool doRefresh = true)
    {
        lock (_clientLock)
        {
            _client = client;

            if (client == null)
            {
                _logger?.Debug("SetClient: {0} argument is null, aborting refresh thread!", nameof(client));

                _refreshThread?.Abort();
                _refreshThread = null;

                return;
            }

            if (doRefresh)
            {
                DoRefresh();

                if (_refreshThread == null)
                {
                    _logger?.Debug("SetClient: refresh thread is null, setting up!");

                    _refreshThread = new Thread(RefreshThread) { IsBackground = true };
                    _refreshThread.Start();
                }
            }
        }
    }

    /// <inheritdoc cref="BaseProvider.GetRawValue(string, out string)"/>
    protected override bool GetRawValue(string key, out string value)
    {
        value = null;

        if (!_cachedValues.TryGetValue(key, out var v)) return false;

        if (v is JsonElement element)
            value = element.GetString();
        else
            value = v.ToString();

        return true;
    }
}
