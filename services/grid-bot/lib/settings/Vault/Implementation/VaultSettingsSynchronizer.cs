namespace Grid.Bot;

using System;
using System.Net;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

using VaultSharp;
using VaultSharp.Core;

using Logging;

/// <summary>
/// Brings a Vault secret in line with the current options structure without overwriting existing values.
/// </summary>
/// <param name="client">The Vault client.</param>
/// <param name="mount">The KV v2 mount point.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
/// <exception cref="ArgumentNullException">
/// - <paramref name="client"/> cannot be null.
/// - <paramref name="logger"/> cannot be null.
/// </exception>
public class VaultSettingsSynchronizer(IVaultClient client, string mount, ILogger logger)
{
    private readonly IVaultClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly string _mount = mount;
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private static object Unwrap(object value) => value switch
    {
        JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
        JsonElement { ValueKind: JsonValueKind.Number } element => element.TryGetInt64(out var number) ? number : element.GetDouble(),
        JsonElement { ValueKind: JsonValueKind.True } => true,
        JsonElement { ValueKind: JsonValueKind.False } => false,
        JsonElement { ValueKind: JsonValueKind.Null } => null,
        JsonElement element => element.GetRawText(),
        _ => value,
    };

    /// <summary>
    /// Adds keys missing from the secret and removes keys no longer in <paramref name="localValues"/>.
    /// Values already in the secret are never changed.
    /// </summary>
    /// <param name="path">The secret path.</param>
    /// <param name="localValues">The current structure, with the local (environment or default) values.</param>
    /// <param name="dryRun">When true, only reports the changes.</param>
    /// <returns>A task that completes when the secret is in sync.</returns>
    public async Task SyncAsync(string path, IReadOnlyDictionary<string, string> localValues, bool dryRun = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(localValues);

        IDictionary<string, object> remote;

        try
        {
            var secret = await _client.V1.Secrets.KeyValue.V2.ReadSecretAsync(mountPoint: _mount, path: path).ConfigureAwait(false);

            remote = secret?.Data?.Data ?? new Dictionary<string, object>();
        }
        catch (VaultApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            _logger.Information("'{0}/{1}' does not exist, it will be created.", _mount, path);

            remote = new Dictionary<string, object>();
        }

        var remoteKeys = new HashSet<string>(remote.Keys, StringComparer.OrdinalIgnoreCase);
        var localKeys = new HashSet<string>(localValues.Keys, StringComparer.OrdinalIgnoreCase);

        var toAdd = localValues.Where(pair => !remoteKeys.Contains(pair.Key)).ToList();
        var toRemove = remote.Keys.Where(key => !localKeys.Contains(key)).ToList();

        foreach (var pair in toAdd)
            _logger.Information("'{0}/{1}': adding '{2}'", _mount, path, pair.Key);

        foreach (var key in toRemove)
            _logger.Information("'{0}/{1}': removing '{2}'", _mount, path, key);

        if (toAdd.Count == 0 && toRemove.Count == 0)
        {
            _logger.Information("'{0}/{1}' is already up to date.", _mount, path);

            return;
        }

        if (dryRun)
        {
            _logger.Information("Dry run, not writing '{0}/{1}'.", _mount, path);

            return;
        }

        var data = remote
            .Where(pair => !toRemove.Contains(pair.Key))
            .ToDictionary(pair => pair.Key, pair => Unwrap(pair.Value));

        foreach (var pair in toAdd)
            data[pair.Key] = pair.Value;

        await _client.V1.Secrets.KeyValue.V2.WriteSecretAsync(mountPoint: _mount, path: path, data: data).ConfigureAwait(false);

        _logger.Information("Wrote '{0}/{1}': {2} added, {3} removed.", _mount, path, toAdd.Count, toRemove.Count);
    }
}
