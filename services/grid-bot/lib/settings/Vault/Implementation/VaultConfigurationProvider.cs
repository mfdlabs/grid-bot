namespace Grid.Bot;

using System;
using System.Net;
using System.Threading;
using System.Text.Json;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

using VaultSharp.Core;

/// <summary>
/// An <see cref="IConfigurationProvider"/> backed by one or more Vault KV v2 secrets.
/// </summary>
public class VaultConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly VaultConfigurationSource _source;
    private readonly Dictionary<string, string>[] _lastValues;
    private readonly Timer _timer;

    private bool _disposed;

    /// <summary>
    /// Construct a new instance of <see cref="VaultConfigurationProvider"/>.
    /// </summary>
    /// <param name="source">The <see cref="VaultConfigurationSource"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> cannot be null.</exception>
    /// <exception cref="ArgumentException">The source has no client, mount or secrets.</exception>
    public VaultConfigurationProvider(VaultConfigurationSource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));

        if (source.Client == null) throw new ArgumentException("Client is required.", nameof(source));
        if (string.IsNullOrWhiteSpace(source.Mount)) throw new ArgumentException("Mount is required.", nameof(source));
        if (source.Secrets.Count == 0) throw new ArgumentException("At least one secret must be mapped.", nameof(source));

        _lastValues = new Dictionary<string, string>[source.Secrets.Count];

        if (source.ReloadInterval is { } interval && interval > TimeSpan.Zero)
            _timer = new Timer(_ => Reload(), null, interval, interval);
    }

    /// <inheritdoc cref="ConfigurationProvider.Load"/>
    public override void Load() => Data = ReadAll(throwOnError: !_source.Optional);

    private void Reload()
    {
        if (_disposed) return;

        try
        {
            var data = ReadAll(throwOnError: false);

            if (SameAs(data)) return;

            Data = data;
            OnReload();
        }
        catch (Exception ex)
        {
            _source.Logger?.Error("VaultConfigurationProvider: Failed to refresh: {0}", ex.Message);
        }
    }

    private bool SameAs(Dictionary<string, string> other)
    {
        if (other.Count != Data.Count) return false;

        foreach (var kvp in other)
            if (!Data.TryGetValue(kvp.Key, out var value) || value != kvp.Value)
                return false;

        return true;
    }

    private Dictionary<string, string> ReadAll(bool throwOnError)
    {
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < _source.Secrets.Count; i++)
        {
            var mapping = _source.Secrets[i];
            var path = GetFullPath(mapping.Path);

            Dictionary<string, string> values;

            try
            {
                values = _lastValues[i] = ReadSecret(path, mapping.Section);
            }
            catch (Exception ex)
            {
                if (throwOnError) throw;

                _source.Logger?.Warning("VaultConfigurationProvider: Failed to read '{0}/{1}': {2}", _source.Mount, path, ex.Message);

                // A failing path keeps its last known good values.
                values = _lastValues[i] ?? [];
            }

            foreach (var kvp in values)
                merged[kvp.Key] = kvp.Value;
        }

        return merged;
    }

    private string GetFullPath(string path)
        => string.IsNullOrWhiteSpace(_source.PathPrefix) ? path : $"{_source.PathPrefix.Trim('/')}/{path.TrimStart('/')}";

    private Dictionary<string, string> ReadSecret(string path, string section)
    {
        var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        IDictionary<string, object> values;

        try
        {
            var secret = _source.Client.V1.Secrets.KeyValue.V2
                .ReadSecretAsync(path: path, mountPoint: _source.Mount)
                .GetAwaiter()
                .GetResult();

            values = secret?.Data?.Data;
        }
        catch (VaultApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return data;
        }

        if (values == null) return data;

        var prefix = string.IsNullOrWhiteSpace(section) ? string.Empty : section + ConfigurationPath.KeyDelimiter;

        foreach (var kvp in values)
            data[prefix + kvp.Key] = ToString(kvp.Value);

        return data;
    }

    private static string ToString(object value) => value switch
    {
        null => null,
        JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
        JsonElement element => element.GetRawText(),
        _ => value.ToString(),
    };

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose()
    {
        if (_disposed) return;

        GC.SuppressFinalize(this);

        _disposed = true;
        _timer?.Dispose();
    }
}
