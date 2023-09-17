namespace Configuration;

using System;
using System.Net;
using System.Threading;
using System.Text.Json;
using System.ComponentModel;
using System.Collections.Generic;

using VaultSharp;
using VaultSharp.Core;

using Logging;
using Threading.Extensions;

/// <summary>
/// Implementation for <see cref="BaseProvider"/> via Vault.
/// </summary>
public class VaultProvider : BaseProvider, IVaultProvider
{
    private static readonly TimeSpan _defaultRefreshInterval = TimeSpan.FromMinutes(10);

    private IDictionary<string, object> _cachedValues = new Dictionary<string, object>();
    private Thread _refreshThread;
    private IVaultClient _client;

    private readonly object _clientLock = new();
    private readonly string _mount;
    private readonly string _path;
    private readonly TimeSpan _refreshInterval;

    /// <inheritdoc cref="IVaultProvider.Mount"/>
    public virtual string Mount => _mount;

    /// <inheritdoc cref="IVaultProvider.Path"/>
    public virtual string Path => _path;

    /// <inheritdoc cref="INotifyPropertyChanged.PropertyChanged"/>
    public override event PropertyChangedEventHandler PropertyChanged;

    /// <inheritdoc cref="BaseProvider.Set{T}(string, T)"/>
    public override void Set<T>(string variable, T value)
    {
        if (_client == null) return;

        _logger?.Debug("Set value in vault at path '{0}/{1}/{2}'", Mount, Path, variable);

        var realValue = value.ToString();

        if (typeof(T).IsArray)
            realValue = string.Join(",", value as Array);

        _cachedValues[variable] = realValue;

        PropertyChanged?.Invoke(this, new(variable));

        _client?.V1.Secrets.KeyValue.V2.WriteSecretAsync(
            mountPoint: Mount,
            path: Path,
            data: _cachedValues
        );
    }

    /// <summary>
    /// Construct a new instance of <see cref="VaultProvider"/>
    /// </summary>
    /// <param name="mount">The base KVv2 path.</param>
    /// <param name="path">The optional path.</param>
    /// <param name="refreshInterval">The refresh interval.</param>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="client">The <see cref="IVaultClient"/></param>
    /// <exception cref="ArgumentException">
    /// - <paramref name="mount"/> cannot be null or empty.
    /// - <paramref name="path"/> cannot be null or empty.
    /// </exception>
    public VaultProvider(
        string mount = "",
        string path = "/",
        TimeSpan? refreshInterval = null,
        ILogger logger = null,
        IVaultClient client = null
    )
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException($"{nameof(path)} cannot be null or empty.", nameof(path));

        _mount = mount;
        _path = path;
        _refreshInterval = refreshInterval ?? _defaultRefreshInterval;

        if (logger != null)
            SetLogger(logger);

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
        if (_cachedValues == null)
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

    /// <inheritdoc cref="IVaultProvider.SetClient(IVaultClient)"/>
    public void SetClient(IVaultClient client = null)
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

            DoRefresh();

            if (_refreshThread == null)
            {
                _logger?.Debug("SetClient: refresh thread is null, setting up!");

                _refreshThread = new Thread(RefreshThread) { IsBackground = true };
                _refreshThread.Start();
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
