namespace Grid.Bot;

using System;

/// <summary>
/// Settings provider for all Discord roles related stuff.
/// </summary>
public class DiscordRolesSettings : BaseSettingsProvider
{
    /// <inheritdoc cref="Configuration.IVaultProvider.Path"/>
    public override string Path => SettingsProvidersDefaults.DiscordRolesPath;

    /// <summary>
    /// Gets or sets the admin user ids.
    /// </summary>
    public ulong[] AdminUserIds
    {
        get => GetOrDefault(nameof(AdminUserIds), Array.Empty<ulong>());
        set => Set(nameof(AdminUserIds), value);
    }

    /// <summary>
    /// Gets or sets the blacklisted user ids.
    /// </summary>
    public ulong[] BlacklistedUserIds
    {
        get => GetOrDefault(nameof(BlacklistedUserIds), Array.Empty<ulong>());
        set => Set(nameof(BlacklistedUserIds), value);
    }

    /// <summary>
    /// Gets or sets the higher privilaged user ids.
    /// </summary>
    public ulong[] HigherPrivilagedUserIds
    {
        get => GetOrDefault(nameof(HigherPrivilagedUserIds), Array.Empty<ulong>());
        set => Set(nameof(HigherPrivilagedUserIds), value);
    }

    /// <summary>
    /// Gets the bot owner id.
    /// </summary>
    public ulong BotOwnerId => GetOrDefault(nameof(BotOwnerId), default(ulong));
}
