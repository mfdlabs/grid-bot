namespace Grid.Bot;

using System;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

using VaultSharp;

using Logging;

/// <summary>
/// Maps a Vault secret path to a configuration section.
/// </summary>
/// <param name="Section">The section, or null for the root.</param>
/// <param name="Path">The secret path relative to the source's prefix.</param>
public sealed record VaultSecretMapping(string Section, string Path);

/// <summary>
/// An <see cref="IConfigurationSource"/> that reads one or more KV v2 secrets from Vault,
/// each placed under its own configuration section.
/// </summary>
public class VaultConfigurationSource : IConfigurationSource
{
    private readonly List<VaultSecretMapping> _secrets = [];

    /// <summary>
    /// Gets or sets the Vault client.
    /// </summary>
    public IVaultClient Client { get; set; }

    /// <summary>
    /// Gets or sets the KV v2 mount point.
    /// </summary>
    public string Mount { get; set; }

    /// <summary>
    /// Gets or sets a prefix (for example the environment name) joined to every mapped path.
    /// </summary>
    public string PathPrefix { get; set; }

    /// <summary>
    /// Gets the secrets to read.
    /// </summary>
    public IReadOnlyList<VaultSecretMapping> Secrets => _secrets;

    /// <summary>
    /// Gets or sets how often the secrets are re-read. Null or non-positive disables reloading.
    /// </summary>
    public TimeSpan? ReloadInterval { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a failed initial read is ignored instead of thrown.
    /// </summary>
    public bool Optional { get; set; }

    /// <summary>
    /// Gets or sets the logger.
    /// </summary>
    public ILogger Logger { get; set; }

    /// <summary>
    /// Maps the secret at <paramref name="path"/> (relative to <see cref="PathPrefix"/>) to a configuration section.
    /// </summary>
    /// <param name="section">The section the secret's keys are placed under, or null for the root.</param>
    /// <param name="path">The secret path.</param>
    /// <returns>This source.</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> is null or whitespace.</exception>
    public VaultConfigurationSource Map(string section, string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));

        _secrets.Add(new(section, path));

        return this;
    }

    /// <inheritdoc/>
    public IConfigurationProvider Build(IConfigurationBuilder builder) => new VaultConfigurationProvider(this);
}
