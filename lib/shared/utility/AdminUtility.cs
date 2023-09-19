namespace Grid.Bot.Utility;

using System.Linq;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using Logging;

/// <summary>
/// Utility class for administration.
/// </summary>
public static class AdminUtility
{
    /// <summary>
    /// Is the <see cref="IUser"/> the owner?.
    /// </summary>
    /// <param name="user">The <see cref="IUser"/></param>
    /// <returns>Returns true if the user's id matches the <see cref="DiscordRolesSettings.BotOwnerId"/></returns>
    public static bool UserIsOwner(IUser user) 
        => user.Id == DiscordRolesSettings.Singleton.BotOwnerId;

    /// <summary>
    /// Is the <see cref="IUser"/> an admin?
    /// </summary>
    /// <param name="user">The <see cref="IUser"/></param>
    /// <returns>Returns true if the user's id is in the <see cref="DiscordRolesSettings.AdminUserIds"/></returns>
    public static bool UserIsAdmin(IUser user) 
        => UserIsOwner(user) || DiscordRolesSettings.Singleton.AdminUserIds.Contains(user.Id);

    /// <summary>
    /// Is the <see cref="IUser"/> a higher privilaged user?
    /// </summary>
    /// <param name="user">The <see cref="IUser"/></param>
    /// <returns>Returns true if the user's id is in the <see cref="DiscordRolesSettings.HigherPrivilagedUserIds"/></returns>
    public static bool UserIsPrivilaged(IUser user)
        => UserIsAdmin(user) || DiscordRolesSettings.Singleton.HigherPrivilagedUserIds.Contains(user.Id);

    /// <summary>
    /// Is the <see cref="IUser"/> blacklisted?
    /// </summary>
    /// <param name="user">The <see cref="IUser"/></param>
    /// <returns>Returns true if the user's id is in the <see cref="DiscordRolesSettings.BlacklistedUserIds"/></returns>
    public static bool UserIsBlacklisted(IUser user)
        => !UserIsOwner(user) && DiscordRolesSettings.Singleton.BlacklistedUserIds.Contains(user.Id);
    
#if WE_LOVE_EM_SLASH_COMMANDS
    /// <summary>
    /// Rejects the <see cref="SocketCommandBase"/> request if the
    /// user is not an admin.
    /// </summary>
    /// <param name="command">The <see cref="SocketCommandBase"/></param>
    /// <returns>True if the user has access, false otherwhise.</returns>
    public static async Task<bool> RejectIfNotAdminAsync(SocketCommandBase command)
    {
        if (!UserIsAdmin(command.User))
        {
            await command.RespondEphemeralPingAsync("You lack the correct permissions to execute that command.");

            return false;
        }

        Logger.Singleton.Information("User '{0}' is an admin.", command.User.Id);

        return true;
    }

    /// <summary>
    /// Rejects the <see cref="SocketCommandBase"/> request if the
    /// user is not the owner.
    /// </summary>
    /// <param name="command">The <see cref="SocketCommandBase"/></param>
    /// <returns>True if the user has access, false otherwhise.</returns>
    public static async Task<bool> RejectIfNotOwnerAsync(SocketCommandBase command)
    {
        if (!UserIsOwner(command.User))
        {
            await command.RespondEphemeralPingAsync("You lack the correct permissions to execute that command.");

            return false;
        }

        Logger.Singleton.Information("User '{0}' is the owner.", command.User.Id);

        return true;
    }
#endif

    /// <summary>
    /// Rejects the <see cref="SocketMessage"/> request if the
    /// user is not an admin.
    /// </summary>
    /// <param name="message">The <see cref="SocketMessage"/></param>
    /// <returns>True if the user has access, false otherwhise.</returns>
    public static async Task<bool> RejectIfNotAdminAsync(SocketMessage message)
    {
        if (!UserIsAdmin(message.Author))
        {
            await message.ReplyAsync("You lack the correct permissions to execute that command.");

            return false;
        }

        Logger.Singleton.Information("User '{0}' is on the admin whitelist.", message.Author.Id);

        return true;
    }

    /// <summary>
    /// Rejects the <see cref="SocketMessage"/> request if the
    /// user is not the owner.
    /// </summary>
    /// <param name="message">The <see cref="SocketMessage"/></param>
    /// <returns>True if the user has access, false otherwhise.</returns>
    public static async Task<bool> RejectIfNotOwnerAsync(SocketMessage message)
    {
        if (!UserIsOwner(message.Author))
        {
            await message.ReplyAsync("You lack the correct permissions to execute that command.");

            return false;
        }

        Logger.Singleton.Information("User '{0}' is the owner.", message.Author.Id);

        return true;
    }
}
