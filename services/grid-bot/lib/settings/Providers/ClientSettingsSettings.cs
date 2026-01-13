namespace Grid.Bot;

using System;
using System.Collections.Generic;

/// <summary>
/// Settings provider for client-settings
/// </summary>
public class ClientSettingsSettings : BaseSettingsProvider
{
    /// <inheritdoc cref="Configuration.IVaultProvider.Path"/>
    public override string Path => SettingsProvidersDefaults.ClientSettingsPath;

    
    /// <summary>
    /// Determines if data access should be via Vault or local JSON files.
    /// </summary>
    public bool ClientSettingsViaVault => GetOrDefault(nameof(ClientSettingsViaVault), false);

    /// <summary>
    /// Gets the Vault mount for the client settings.
    /// </summary>
    public string ClientSettingsVaultMount => GetOrDefault(nameof(ClientSettingsVaultMount), "client-settings");
    
    /// <summary>
    /// Gets the absolute path to the client settings vault path.
    /// </summary>
    public string ClientSettingsVaultPath => GetOrDefault(nameof(ClientSettingsVaultPath), "/");

    /// <summary>
    /// Gets the Vault address for the client settings.
    /// </summary>
    public string ClientSettingsVaultAddress => GetOrDefault(nameof(ClientSettingsVaultAddress), Environment.GetEnvironmentVariable("VAULT_ADDR"));

    /// <summary>
    /// Gets the Vault token for the client settings.
    /// </summary>
    public string ClientSettingsVaultToken => GetOrDefault(nameof(ClientSettingsVaultToken), Environment.GetEnvironmentVariable("VAULT_TOKEN"));

    /// <summary>
    /// Gets the refresh interval for the client settings factory.
    /// </summary>
    public TimeSpan ClientSettingsRefreshInterval => GetOrDefault(nameof(ClientSettingsRefreshInterval), TimeSpan.FromSeconds(30));

    /// <summary>
    /// Gets the absolute path to the client settings configuration file if <see cref="ClientSettingsViaVault"/> is false.
    /// </summary>
    public string ClientSettingsFilePath => GetOrDefault(nameof(ClientSettingsFilePath), "/var/cache/mfdlabs/client-settings.json");

    /// <summary>
    /// Resolves the dependency maps for specific application settings.
    /// </summary>
    /// <value>
    /// The value is formatted like this:
    /// <code>
    /// Group1=Group2,Group3
    /// Group2=Group4
    /// </code>
    /// </value>
    public Dictionary<string, string> ClientSettingsApplicationDependencies => GetOrDefault(nameof(ClientSettingsApplicationDependencies), new Dictionary<string, string>());

    /// <summary>
    /// A list of application names that can be read from the API endpoints.
    /// </summary>
    public string[] PermissibleReadApplications
    {
        get => GetOrDefault(nameof(PermissibleReadApplications), Array.Empty<string>());
        set => Set(nameof(PermissibleReadApplications), value);
    }

    /// <summary>
    /// Gets a command separated list of API keys that can be used for reading non-permissible applications,
    /// as well as executing privalaged commands.
    /// </summary>
    /// <remarks>If this is empty, it will leave endpoints open!!!</remarks>
    public string[] ClientSettingsApiKeys => GetOrDefault(nameof(ClientSettingsApiKeys), Array.Empty<string>());

}
