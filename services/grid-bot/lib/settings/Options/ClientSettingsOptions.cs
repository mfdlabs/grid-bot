namespace Grid.Bot;

using System;
using System.Collections.Generic;

/// <summary>
/// Options for client-settings.
/// </summary>
public class ClientSettingsOptions
{
    /// <summary>
    /// The configuration section these options bind from.
    /// </summary>
    public const string SectionName = "ClientSettings";

    /// <summary>
    /// The vault path for the client settings options.
    /// </summary>
    public const string VaultPath = "client-settings";

    /// <summary>
    /// Gets or sets a value indicating whether data access is via Vault instead of local JSON files.
    /// </summary>
    public bool ClientSettingsViaVault { get; set; }

    /// <summary>
    /// Gets or sets the Vault mount for the client settings.
    /// </summary>
    public string ClientSettingsVaultMount { get; set; } = "client-settings";

    /// <summary>
    /// Gets or sets the absolute path to the client settings vault path.
    /// </summary>
    public string ClientSettingsVaultPath { get; set; } = "/";

    /// <summary>
    /// Gets or sets the Vault address override for the client settings, or null to use <see cref="GlobalOptions.VaultAddress"/>.
    /// </summary>
    public string ClientSettingsVaultAddress { get; set; }

    /// <summary>
    /// Gets or sets the Vault credential override for the client settings, or null to use <see cref="GlobalOptions.VaultCredential"/>.
    /// </summary>
    public string ClientSettingsVaultToken { get; set; }

    /// <summary>
    /// Gets or sets the refresh interval for the client settings factory.
    /// </summary>
    public TimeSpan ClientSettingsRefreshInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the absolute path to the client settings configuration file when <see cref="ClientSettingsViaVault"/> is false.
    /// </summary>
    public string ClientSettingsFilePath { get; set; } = "/var/cache/mfdlabs/client-settings.json";

    /// <summary>
    /// Gets or sets the dependency maps for specific application settings.
    /// </summary>
    /// <remarks>
    /// Written at runtime through <see cref="ISettingsWriter"/>. As a single value, one pair per line:
    /// <code>
    /// Group1=Group2,Group3
    /// Group2=Group4
    /// </code>
    /// </remarks>
    public Dictionary<string, string> ClientSettingsApplicationDependencies { get; set; } = [];

    /// <summary>
    /// Gets or sets a list of application names that can be read from the API endpoints.
    /// </summary>
    /// <remarks>Written at runtime through <see cref="ISettingsWriter"/>.</remarks>
    public string[] PermissibleReadApplications { get; set; } = [];

    /// <summary>
    /// Gets or sets API keys that can be used for reading non-permissible applications,
    /// as well as executing privileged commands.
    /// </summary>
    /// <remarks>If this is empty, it will leave endpoints open!!!</remarks>
    public string[] ClientSettingsApiKeys { get; set; } = [];
}
