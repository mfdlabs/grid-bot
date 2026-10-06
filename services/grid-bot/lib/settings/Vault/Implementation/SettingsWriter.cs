namespace Grid.Bot;

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using VaultSharp;
using VaultSharp.V1.SecretsEngines.KeyValue.V2;

using Microsoft.Extensions.Configuration;

using Logging;

/// <summary>
/// Default <see cref="ISettingsWriter"/>.
/// </summary>
/// <param name="client">The Vault client, or null when Vault is not in use.</param>
/// <param name="mount">The Vault KV v2 mount point.</param>
/// <param name="runtime">The runtime configuration provider.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
/// <exception cref="ArgumentNullException">
/// - <paramref name="runtime"/> cannot be null.
/// - <paramref name="logger"/> cannot be null.
/// </exception>
public class SettingsWriter(IVaultClient client, string mount, RuntimeConfigurationProvider runtime, ILogger logger) : ISettingsWriter
{
    private readonly IVaultClient _client = client;
    private readonly string _mount = mount;
    private readonly RuntimeConfigurationProvider _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc cref="ISettingsWriter.SetAsync(string, string, string)"/>
    public Task SetAsync(string section, string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

        return SetAsync(section, new Dictionary<string, string> { [key] = value });
    }

    /// <inheritdoc cref="ISettingsWriter.SetAsync(string, IReadOnlyDictionary{string, string})"/>
    public async Task SetAsync(string section, IReadOnlyDictionary<string, string> values)
    {
        if (string.IsNullOrWhiteSpace(section)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(section));
        ArgumentNullException.ThrowIfNull(values);

        if (!GridBotVaultConfigurationExtensions.SectionPaths.TryGetValue(section, out var path))
            throw new ArgumentException($"Section '{section}' is not mapped to a Vault path.", nameof(section));

        _runtime.SetMany(values.Select(pair => new KeyValuePair<string, string>(ConfigurationPath.Combine(section, pair.Key), pair.Value)));

        if (_client == null) return;

        _logger.Information("Writing '{0}' to Vault at '{1}/{2}'", string.Join(", ", values.Keys), _mount, path);

        await _client.V1.Secrets.KeyValue.V2.PatchSecretAsync(
            path,
            new PatchSecretDataRequest { Data = values.ToDictionary(pair => pair.Key, pair => (object)pair.Value) },
            _mount
        ).ConfigureAwait(false);
    }
}
