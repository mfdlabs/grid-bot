namespace Grid.Bot.Interactions.Private;

using System;
using System.Threading.Tasks;

using Discord;
using Discord.Interactions;

using Utility;

/// <summary>
/// Interaction handler for updating user bot roles.
/// </summary>
/// <remarks>
/// Construct a new instance of <see cref="Roles"/>.
/// </remarks>
/// <param name="discordRolesSettings">The <see cref="DiscordRolesSettings"/>.</param>
/// <param name="adminUtility">The <see cref="IAdminUtility"/>.</param>
/// <exception cref="ArgumentNullException">
/// - <paramref name="discordRolesSettings"/> cannot be null.
/// - <paramref name="adminUtility"/> cannot be null.
/// </exception>
[Group("role", "Commands used for updating user bot roles.")]
[RequireBotRole(BotRole.Administrator)]
public class Roles(
    DiscordRolesSettings discordRolesSettings,
    IAdminUtility adminUtility
) : InteractionModuleBase<ShardedInteractionContext>
{
    private readonly DiscordRolesSettings _discordRolesSettings = discordRolesSettings ?? throw new ArgumentNullException(nameof(discordRolesSettings));
    private readonly IAdminUtility _adminUtility = adminUtility ?? throw new ArgumentNullException(nameof(adminUtility));


    /// <summary>
    /// Updates the user's bot role.
    /// </summary>
    /// <param name="user">The user to update.</param>
    /// <param name="role">The role to update to.</param>
    [SlashCommand("update", "Updates the user's bot role.")]
    public async Task UpdateRoleAsync(
        [Summary("user", "The user to update.")]
        IUser user,
        [Summary("role", "The role to update to.")]
        BotRole role = BotRole.Default
    )
    {
        if (user.IsBot)
        {
            await FollowupAsync("Cannot update a bot's role.");

            return;
        }

        if (_adminUtility.UserIsOwner(user))
        {
            await FollowupAsync("Cannot update the owner's role.");

            return;
        }

        if (role == BotRole.Owner)
        {
            await FollowupAsync("Cannot update a user to the owner role.");

            return;
        }

        switch (role)
        {
            case BotRole.Default:
                _adminUtility.SetUserAsNormal(user);

                break;
            case BotRole.Privileged:
                _adminUtility.SetUserAsPrivilaged(user);

                break;
            case BotRole.Administrator:
                _adminUtility.SetUserAsAdmin(user);

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(role), role, null);
        }

        await FollowupAsync($"Updated {user.Username}'s role to {role}.");
    }

    /// <summary>
    /// Blacklists the user.
    /// </summary>
    /// <param name="user">The user to blacklist.</param>
    [SlashCommand("blacklist", "Blacklists the user.")]
    public async Task BlacklistUserAsync(
        [Summary("user", "The user to blacklist.")]
        IUser user
    )
    {
        if (user.IsBot)
        {
            await FollowupAsync("Cannot blacklist a bot.");

            return;
        }

        if (_adminUtility.UserIsOwner(user))
        {
            await FollowupAsync("Cannot blacklist the owner.");

            return;
        }

        if (_adminUtility.UserIsBlacklisted(user))
        {
            await FollowupAsync("User is already blacklisted.");

            return;
        }

        _adminUtility.BlacklistUser(user);

        await FollowupAsync($"Blacklisted {user.Username}.");
    }

    /// <summary>
    /// Unblacklists the user.
    /// </summary>
    /// <param name="user">The user to unblacklist.</param>
    [SlashCommand("unblacklist", "Unblacklists the user.")]
    public async Task UnblacklistUserAsync(
        [Summary("user", "The user to unblacklist.")]
        IUser user
    )
    {
        if (user.IsBot)
        {
            await FollowupAsync("Cannot unblacklist a bot.");

            return;
        }

        if (_adminUtility.UserIsOwner(user))
        {
            await FollowupAsync("Cannot unblacklist the owner.");

            return;
        }

        if (!_adminUtility.UserIsBlacklisted(user))
        {
            await FollowupAsync("User is not blacklisted.");

            return;
        }

        _adminUtility.UnblacklistUser(user);

        await FollowupAsync($"Unblacklisted {user.Username}.");
    }
}
