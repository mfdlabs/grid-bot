namespace Grid.Bot;

/// <summary>
/// Options for all Discord roles related settings.
/// </summary>
public class DiscordRolesOptions
{
    /// <summary>
    /// The configuration section these options bind from.
    /// </summary>
    public const string SectionName = "DiscordRoles";

    /// <summary>
    /// The vault path for the Discord roles options.
    /// </summary>
    public const string VaultPath = "discord-roles";

    /// <summary>
    /// Gets or sets the admin user ids.
    /// </summary>
    /// <remarks>Written at runtime through <see cref="ISettingsWriter"/>.</remarks>
    public ulong[] AdminUserIds { get; set; } = [];

    /// <summary>
    /// Gets or sets the blacklisted user ids.
    /// </summary>
    /// <remarks>Written at runtime through <see cref="ISettingsWriter"/>.</remarks>
    public ulong[] BlacklistedUserIds { get; set; } = [];

    /// <summary>
    /// Gets or sets the higher privileged user ids.
    /// </summary>
    /// <remarks>Written at runtime through <see cref="ISettingsWriter"/>.</remarks>
    public ulong[] HigherPrivilagedUserIds { get; set; } = [];

    /// <summary>
    /// Gets or sets the bot owner id.
    /// </summary>
    public ulong BotOwnerId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the role that is used to create notifications for alerts.
    /// </summary>
    public ulong AlertRoleId { get; set; }
}
