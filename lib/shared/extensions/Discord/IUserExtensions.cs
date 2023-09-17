namespace Grid.Bot.Extensions;

using System.Linq;
using System.Threading.Tasks;

using Discord;

using Random;

using Global;
using Utility;

/// <summary>
/// Extension methods for <see cref="IUser"/>
/// </summary>
public static class IUserExtensions
{
    /// <summary>
    /// Whitelist the specified <see cref="IUser"/>
    /// </summary>
    /// <param name="user">The <see cref="IUser"/></param>
    public static void Whitelist(this IUser user)
    {
        if (user.IsBlacklisted())
        {
            var blIds = DiscordRolesSettings.Singleton.BlacklistedUserIds.ToList();
            blIds.Remove(user.Id);

            DiscordRolesSettings.Singleton.BlacklistedUserIds = blIds.ToArray();
        }
    }

    /// <summary>
    /// Blacklist the specified <see cref="IUser"/>
    /// </summary>
    /// <param name="user">The <see cref="IUser"/></param>
    public static void Blacklist(this IUser user)
    {
        if (!user.IsBlacklisted())
        {
            var blIds = DiscordRolesSettings.Singleton.BlacklistedUserIds.ToList();
            blIds.Add(user.Id);

            DiscordRolesSettings.Singleton.BlacklistedUserIds = blIds.ToArray();
        }
    }

    /// <summary>
    /// Disentitle the specified <see cref="IUser"/>
    /// </summary>
    /// <param name="user">The <see cref="IUser"/></param>
    public static void Disentitle(this IUser user)
    {
        if (user.IsPrivilaged())
        {
            var blIds = DiscordRolesSettings.Singleton.HigherPrivilagedUserIds.ToList();
            blIds.Remove(user.Id);

            DiscordRolesSettings.Singleton.HigherPrivilagedUserIds = blIds.ToArray();
        }
    }

    /// <summary>
    /// Entitle the specified <see cref="IUser"/>
    /// </summary>
    /// <param name="user">The <see cref="IUser"/></param>
    public static void Entitle(this IUser user)
    {
        if (!user.IsPrivilaged())
        {
            var blIds = DiscordRolesSettings.Singleton.HigherPrivilagedUserIds.ToList();
            blIds.Add(user.Id);

            DiscordRolesSettings.Singleton.HigherPrivilagedUserIds = blIds.ToArray();
        }
    }

    /// <summary>
    /// Demote the specified <see cref="IUser"/>
    /// </summary>
    /// <param name="user">The <see cref="IUser"/></param>
    public static void Demote(this IUser user)
    {
        if (user.IsAdmin())
        {
            var blIds = DiscordRolesSettings.Singleton.AdminUserIds.ToList();
            blIds.Remove(user.Id);

            DiscordRolesSettings.Singleton.AdminUserIds = blIds.ToArray();
        }
    }

    /// <summary>
    /// Promote the specified <see cref="IUser"/>
    /// </summary>
    /// <param name="user">The <see cref="IUser"/></param>
    public static void Promote(this IUser user)
    {
        if (!user.IsAdmin())
        {
            var blIds = DiscordRolesSettings.Singleton.AdminUserIds.ToList();
            blIds.Add(user.Id);

            DiscordRolesSettings.Singleton.AdminUserIds = blIds.ToArray();
        }
    }

    /// <summary>
    /// Can the specified <see cref="IUser"/> execute by rollout percentage?
    /// </summary>
    /// <remarks>
    ///     Admin users and higher can always execute.
    /// </remarks>
    /// <param name="user">The <see cref="IUser"/></param>
    /// <param name="rolloutPercentage">The rollout percentage.</param>
    /// <returns>True if the user can execute.</returns>
    public static bool CanExecuteByRolloutPercentage(this IUser user, int rolloutPercentage)
    {
        if (user.IsAdmin()) return true;

        return PercentageInvoker.Singleton.CanInvoke(rolloutPercentage);
    }

    /// <summary>
    /// Is the specified <see cref="IUser"/> blacklisted?
    /// </summary>
    /// <param name="user">The <see cref="IUser"/></param>
    /// <returns>True if the user is blacklisted.</returns>
    public static bool IsBlacklisted(this IUser user) => AdminUtility.UserIsBlacklisted(user);

    /// <summary>
    /// Is the specified <see cref="IUser"/> an admin?
    /// </summary>
    /// <param name="user">The <see cref="IUser"/></param>
    /// <returns>True if the user is an admin.</returns>
    public static bool IsAdmin(this IUser user) => AdminUtility.UserIsAdmin(user);

    /// <summary>
    /// Is the specified <see cref="IUser"/> privilaged?
    /// </summary>
    /// <param name="user">The <see cref="IUser"/></param>
    /// <returns>True if the user is privilaged.</returns>
    public static bool IsPrivilaged(this IUser user) => AdminUtility.UserIsPrivilaged(user);

    /// <summary>
    /// Is the specified <see cref="IUser"/> the owner?
    /// </summary>
    /// <param name="user">The <see cref="IUser"/></param>
    /// <returns>True if the user is the owner.</returns>
    public static bool IsOwner(this IUser user) => AdminUtility.UserIsOwner(user);

    /// <summary>
    /// Sends a direct message to the specified <see cref="IUser"/>.
    /// </summary>
    /// <param name="user">The <see cref="IUser"/></param>
    /// <param name="text">The message text.</param>
    /// <param name="isTts">Is the message a TTS message?</param>
    /// <param name="embed">The <see cref="Embed"/></param>
    /// <param name="options">The <see cref="RequestOptions"/></param>
    /// <param name="messageReference">The <see cref="MessageReference"/></param>
    /// <returns>A <see cref="IUserMessage"/></returns>
    public static async Task<IUserMessage> SendDirectMessageAsync(
        this IUser user,
        string text = null,
        bool isTts = false,
        Embed embed = null,
        RequestOptions options = null,
        MessageReference messageReference = null
    )
    {
        var dmChannel = await BotRegistry.Client.GetDMChannelAsync(user.Id) ?? await user.CreateDMChannelAsync();

        return await dmChannel?.SendMessageAsync(
            text,
            isTts,
            embed,
            options,
            new AllowedMentions(AllowedMentionTypes.Users) { MentionRepliedUser = true },
            messageReference
        );
    }
    
}
