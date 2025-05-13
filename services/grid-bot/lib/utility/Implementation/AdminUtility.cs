namespace Grid.Bot.Utility;

using System;
using System.Linq;

using Prometheus;

using Discord;

/// <summary>
/// Utility class for administration.
/// </summary>
/// <remarks>
/// Construct a new instance of <see cref="AdminUtility"/>.
/// </remarks>
/// <param name="discordRolesSettings">The <see cref="DiscordRolesSettings"/>.</param>
/// <exception cref="ArgumentNullException"><paramref name="discordRolesSettings"/> cannot be null.</exception>
public class AdminUtility(DiscordRolesSettings discordRolesSettings) : IAdminUtility
{
    private readonly DiscordRolesSettings _discordRolesSettings = discordRolesSettings ?? throw new ArgumentNullException(nameof(discordRolesSettings));

    private static readonly Counter _usersBlacklistedCounter = Metrics.CreateCounter("admin_users_blacklisted_total", "Total number of users blacklisted.");
    private static readonly Counter _usersUnblacklistedCounter = Metrics.CreateCounter("admin_users_unblacklisted_total", "Total number of users unblacklisted.");
    private static readonly Counter _usersPrivilagedCounter = Metrics.CreateCounter("admin_users_privilaged_total", "Total number of users with privilaged access.");
    private static readonly Counter _adminUsersDemotedCounter = Metrics.CreateCounter("admin_users_demoted_total", "Total number of users demoted from admin.");
    private static readonly Counter _privilegedUsersDemotedCounter = Metrics.CreateCounter("admin_users_demoted_privilaged_total", "Total number of users demoted from privilaged.");
    private static readonly Counter _usersPromotedCounter = Metrics.CreateCounter("admin_users_promoted_total", "Total number of users promoted to admin.");

    /// <inheritdoc cref="IAdminUtility.IsInRole(IUser, BotRole)"/>
    public bool IsInRole(IUser user, BotRole botRole = BotRole.Default)
    {
        return botRole switch
        {
            BotRole.Default => true,
            BotRole.Privileged => UserIsPrivilaged(user),
            BotRole.Administrator => UserIsAdmin(user),
            BotRole.Owner => UserIsOwner(user),
            _ => true,
        };
    }

    /// <inheritdoc cref="IAdminUtility.UserIsOwner(IUser)"/>
    public bool UserIsOwner(IUser user) 
        => user.Id == _discordRolesSettings.BotOwnerId;

    /// <inheritdoc cref="IAdminUtility.UserIsAdmin(IUser)"/>
    public bool UserIsAdmin(IUser user) 
        => UserIsOwner(user) || _discordRolesSettings.AdminUserIds.Contains(user.Id);

    /// <inheritdoc cref="IAdminUtility.UserIsPrivilaged(IUser)"/>
    public bool UserIsPrivilaged(IUser user)
        => UserIsAdmin(user) || _discordRolesSettings.HigherPrivilagedUserIds.Contains(user.Id);

    /// <inheritdoc cref="IAdminUtility.UserIsBlacklisted(IUser)"/>
    public bool UserIsBlacklisted(IUser user)
        => !UserIsOwner(user) && _discordRolesSettings.BlacklistedUserIds.Contains(user.Id);

    /// <inheritdoc cref="IAdminUtility.SetUserAsPrivilaged(IUser)"/>
    public void SetUserAsPrivilaged(IUser user)
    {
        if (UserIsAdmin(user))
        {
            _adminUsersDemotedCounter.Inc();

            // Remove from admin
            var adminUserIds = _discordRolesSettings.AdminUserIds.ToList();

            adminUserIds.Remove(user.Id);

            _discordRolesSettings.AdminUserIds = adminUserIds.ToArray();
        }

        if (!UserIsPrivilaged(user))
        {
            _usersPrivilagedCounter.Inc();

            // Add to privilaged
            var higherPrivilagedUserIds = _discordRolesSettings.HigherPrivilagedUserIds.ToList();

            higherPrivilagedUserIds.Add(user.Id);

            _discordRolesSettings.HigherPrivilagedUserIds = higherPrivilagedUserIds.ToArray();
        }
    }

    /// <inheritdoc cref="IAdminUtility.SetUserAsNormal(IUser)"/>
    public void SetUserAsNormal(IUser user)
    {
        if (UserIsAdmin(user))
        {
            _adminUsersDemotedCounter.Inc();

            // Remove from admin
            var adminUserIds = _discordRolesSettings.AdminUserIds.ToList();

            adminUserIds.Remove(user.Id);

            _discordRolesSettings.AdminUserIds = adminUserIds.ToArray();
        }

        if (UserIsPrivilaged(user))
        {
            _privilegedUsersDemotedCounter.Inc();

            // Remove from privilaged
            var higherPrivilagedUserIds = _discordRolesSettings.HigherPrivilagedUserIds.ToList();

            higherPrivilagedUserIds.Remove(user.Id);

            _discordRolesSettings.HigherPrivilagedUserIds = higherPrivilagedUserIds.ToArray();
        }
    }

    /// <inheritdoc cref="IAdminUtility.BlacklistUser(IUser)"/>
    public void BlacklistUser(IUser user)
    {
        if (!UserIsBlacklisted(user))
        {
            _usersBlacklistedCounter.Inc();

            // Add to blacklist
            var blacklistedUserIds = _discordRolesSettings.BlacklistedUserIds.ToList();

            blacklistedUserIds.Add(user.Id);

            _discordRolesSettings.BlacklistedUserIds = blacklistedUserIds.ToArray();
        }
    }

    /// <inheritdoc cref="IAdminUtility.UnblacklistUser(IUser)"/>
    public void UnblacklistUser(IUser user)
    {
        if (UserIsBlacklisted(user))
        {
            _usersUnblacklistedCounter.Inc();

            // Remove from blacklist
            var blacklistedUserIds = _discordRolesSettings.BlacklistedUserIds.ToList();

            blacklistedUserIds.Remove(user.Id);

            _discordRolesSettings.BlacklistedUserIds = blacklistedUserIds.ToArray();
        }
    }

    /// <inheritdoc cref="IAdminUtility.SetUserAsAdmin(IUser)"/>
    public void SetUserAsAdmin(IUser user)
    {
        if (!UserIsAdmin(user) && UserIsPrivilaged(user))
        {
            _privilegedUsersDemotedCounter.Inc();

            // Remove from privilaged
            var higherPrivilagedUserIds = _discordRolesSettings.HigherPrivilagedUserIds.ToList();

            higherPrivilagedUserIds.Remove(user.Id);

            _discordRolesSettings.HigherPrivilagedUserIds = higherPrivilagedUserIds.ToArray();
        }

        if (!UserIsAdmin(user))
        {
            _usersPromotedCounter.Inc();

            // Add to admin
            var adminUserIds = _discordRolesSettings.AdminUserIds.ToList();

            adminUserIds.Add(user.Id);

            _discordRolesSettings.AdminUserIds = adminUserIds.ToArray();
        }
    }
}
