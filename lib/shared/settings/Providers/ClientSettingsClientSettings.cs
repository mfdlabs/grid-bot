namespace Grid.Bot;

/// <summary>
/// Settings provider for client-settings
/// </summary>
public class ClientSettingsClientSettings : BaseSettingsProvider
{
    /// <inheritdoc cref="Configuration.IVaultProvider.Path"/>
    public override string Path => SettingsProvidersDefaults.ClientSettingsClientPath;

    /// <summary>
    /// Gets the base url for the client settings API.
    /// </summary>
    public string ClientSettingsApiBaseUrl => GetOrDefault(
        nameof(ClientSettingsApiBaseUrl),
        "https://clientsettingscdn.sitetest4.robloxlabs.com/"
    );

    /// <summary>
    /// Should certificate validation be enabled when interacting with the client settings API?
    /// </summary>
    public bool ClientSettingsCertificateValidationEnabled => GetOrDefault(
        nameof(ClientSettingsCertificateValidationEnabled),
        false
    );

    /// <summary>
    /// Gets the API key used to interact with the client settings API.
    /// </summary>
    public string ClientSettingsApiKey => GetOrDefault(
        nameof(ClientSettingsApiKey),
        string.Empty
    );
}
