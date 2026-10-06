namespace Grid.Bot;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Writes a setting at runtime. The value takes effect immediately and is persisted to Vault when Vault is in use.
/// </summary>
/// <remarks>
/// Runtime writes sit above Vault but below appsettings.json and environment variables,
/// so a locally configured value always wins.
/// </remarks>
public interface ISettingsWriter
{
    /// <summary>
    /// Sets <paramref name="key"/> within <paramref name="section"/>.
    /// </summary>
    /// <param name="section">The options section, for example <see cref="ScriptsOptions.SectionName"/>.</param>
    /// <param name="key">The setting name.</param>
    /// <param name="value">The value. Lists are comma separated.</param>
    /// <returns>A task that completes when the value has been persisted.</returns>
    /// <exception cref="ArgumentException">The section is not mapped to a Vault path.</exception>
    Task SetAsync(string section, string key, string value);

    /// <summary>
    /// Sets several keys within <paramref name="section"/> as a single write.
    /// </summary>
    /// <param name="section">The options section.</param>
    /// <param name="values">The setting names and values.</param>
    /// <returns>A task that completes when the values have been persisted.</returns>
    /// <exception cref="ArgumentException">The section is not mapped to a Vault path.</exception>
    Task SetAsync(string section, IReadOnlyDictionary<string, string> values);
}
