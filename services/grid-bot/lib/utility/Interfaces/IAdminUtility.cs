namespace Grid.Bot.Utility;

using Discord;

/// <summary>
/// Utility class for administration.
/// </summary>
public interface IAdminUtility
{
    /// <summary>
    /// Is the specified <see cref="IUser"/> in the specified role?
    /// </summary>
    /// <param name="user">The <see cref="IUser"/></param>
    /// <param name="role">The <see cref="BotRole"/></param>
    /// <returns>Returns true if the user's id matches one of any entry for the specified bot role.</returns>
    bool IsInRole(IUser user, BotRole role = BotRole.Default);

    /// <summary>
    /// Is the <see cref="IUser"/> the owner?.
    /// </summary>
    /// <param name="user">The <see cref="IUser"/></param>
    /// <returns>Returns true if the user's id matches the <see cref="DiscordRolesSettings.BotOwnerId"/></returns>
    bool UserIsOwner(IUser user);

    /// <summary>
    /// Is the <see cref="IUser"/> an admin?
    /// </summary>
    /// <param name="user">The <see cref="IUser"/></param>
    /// <returns>Returns true if the user's id is in the <see cref="DiscordRolesSettings.AdminUserIds"/></returns>
    bool UserIsAdmin(IUser user);

    /// <summary>
    /// Is the <see cref="IUser"/> a higher privilaged user?
    /// </summary>
    /// <param name="user">The <see cref="IUser"/></param>
    /// <returns>Returns true if the user's id is in the <see cref="DiscordRolesSettings.HigherPrivilagedUserIds"/></returns>
    bool UserIsPrivilaged(IUser user);

    /// <summary>
    /// Is the <see cref="IUser"/> blacklisted?
    /// </summary>
    /// <param name="user">The <see cref="IUser"/></param>
    /// <returns>Returns true if the user's id is in the <see cref="DiscordRolesSettings.BlacklistedUserIds"/></returns>
    bool UserIsBlacklisted(IUser user);

    /// <summary>
    /// Set the <see cref="IUser"/> as privileged.
    /// </summary>
    /// <param name="user">The <see cref="IUser"/></param>
    void SetUserAsPrivilaged(IUser user);

    /// <summary>
    /// Set the <see cref="IUser"/> as normal.
    /// </summary>
    /// <param name="user">The <see cref="IUser"/></param>
    void SetUserAsNormal(IUser user);

    /// <summary>
    /// Blacklist the <see cref="IUser"/>.
    /// </summary>
    /// <param name="user">The <see cref="IUser"/></param>
    void BlacklistUser(IUser user);

    /// <summary>
    /// Unblacklist the <see cref="IUser"/>.
    /// </summary>
    /// <param name="user">The <see cref="IUser"/></param>
    void UnblacklistUser(IUser user);

    /// <summary>
    /// Set the <see cref="IUser"/> as admin.
    /// </summary>
    /// <param name="user">The <see cref="IUser"/></param>
    void SetUserAsAdmin(IUser user);
}
