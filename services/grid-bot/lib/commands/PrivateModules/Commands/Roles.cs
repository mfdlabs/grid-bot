namespace Grid.Bot.Commands.Private;

using System;
using System.Threading.Tasks;

using Discord;

using Discord.Commands;

using Utility;
using Extensions;

/// <summary>
/// Command handler for updating user bot roles.
/// </summary>
/// <remarks>
/// Construct a new instance of <see cref="Roles"/>.
/// </remarks>
/// <param name="adminUtility">The <see cref="IAdminUtility"/>.</param>
/// <exception cref="ArgumentNullException"><paramref name="adminUtility"/> cannot be null.</exception>
[LockDownCommand]
[RequireBotRole(BotRole.Administrator)]
[Group("role"), Summary("Commands used for updating user bot roles."), Alias("roles")]
public class Roles(IAdminUtility adminUtility) : ModuleBase
{
    private readonly IAdminUtility _adminUtility = adminUtility ?? throw new ArgumentNullException(nameof(adminUtility));


    /// <summary>
    /// Updates the user's bot role.
    /// </summary>
    /// <param name="user">The user to update.</param>
    /// <param name="role">The role to update to.</param>
    [Command("update"), Summary("Updates the user's bot role.")]
    public async Task UpdateRoleAsync(IUser user, BotRole role = BotRole.Default)
    {
        if (user.IsBot)
        {
            await this.ReplyWithReferenceAsync("Cannot update a bot's role.");

            return;
        }

        if (_adminUtility.UserIsOwner(user))
        {
            await this.ReplyWithReferenceAsync("Cannot update the owner's role.");

            return;
        }

        if (role == BotRole.Owner)
        {
            await this.ReplyWithReferenceAsync("Cannot update a user to the owner role.");

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
            case BotRole.Owner:
            default:
                throw new ArgumentOutOfRangeException(nameof(role), role, null);
        }

        await this.ReplyWithReferenceAsync($"Updated {user.Username}'s role to {role}.");
    }

    /// <summary>
    /// Blacklists the user.
    /// </summary>
    /// <param name="user">The user to blacklist.</param>
    [Command("blacklist"), Summary("Blacklists the user.")]
    public async Task BlacklistUserAsync(IUser user)
    {
        if (user.IsBot)
        {
            await this.ReplyWithReferenceAsync("Cannot blacklist a bot.");

            return;
        }

        if (_adminUtility.UserIsOwner(user))
        {
            await this.ReplyWithReferenceAsync("Cannot blacklist the owner.");

            return;
        }

        if (_adminUtility.UserIsBlacklisted(user))
        {
            await this.ReplyWithReferenceAsync("User is already blacklisted.");

            return;
        }

        _adminUtility.BlacklistUser(user);

        await this.ReplyWithReferenceAsync($"Blacklisted {user.Username}.");
    }

    /// <summary>
    /// Unblacklists the user.
    /// </summary>
    /// <param name="user">The user to unblacklist.</param>
    [Command("unblacklist"), Summary("Unblacklists the user.")]
    public async Task UnblacklistUserAsync(IUser user)
    {
        if (user.IsBot)
        {
            await this.ReplyWithReferenceAsync("Cannot unblacklist a bot.");

            return;
        }

        if (_adminUtility.UserIsOwner(user))
        {
            await this.ReplyWithReferenceAsync("Cannot unblacklist the owner.");

            return;
        }

        if (!_adminUtility.UserIsBlacklisted(user))
        {
            await this.ReplyWithReferenceAsync("User is not blacklisted.");

            return;
        }

        _adminUtility.UnblacklistUser(user);

        await this.ReplyWithReferenceAsync($"Unblacklisted {user.Username}.");
    }
}
