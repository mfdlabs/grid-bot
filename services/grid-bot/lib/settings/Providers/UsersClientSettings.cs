namespace Grid.Bot;

/// <summary>
/// Settings provider for settings used by the Roblox Users API.
/// </summary>
public class UsersClientSettings : BaseSettingsProvider
{
    /// <inheritdoc cref="Configuration.IVaultProvider.Path"/>
    public override string Path => SettingsProvidersDefaults.UsersClientPath;

    /// <summary>
    /// Gets the base url for the Users ApiSite.
    /// </summary>
    public string UsersApiBaseUrl => GetOrDefault(
        nameof(UsersApiBaseUrl),
        "https://users.roblox.com"
    );
}
