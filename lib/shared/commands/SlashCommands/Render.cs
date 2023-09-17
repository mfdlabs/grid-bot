#if WE_LOVE_EM_SLASH_COMMANDS

namespace Grid.Bot.SlashCommands;

using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.WebSocket;

using Logging;

using Text.Extensions;

using Utility;
using Interfaces;
using Extensions;

/// <summary>
/// Renders a Roblox character.
/// </summary>
internal class Render : ISlashCommandHandler
{
    /// <inheritdoc cref="ISlashCommandHandler.Description"/>
    public string Description => "Renders a roblox user.";

    /// <inheritdoc cref="ISlashCommandHandler.Name"/>
    public string Name => "render";

    /// <inheritdoc cref="ISlashCommandHandler.IsInternal"/>
    public bool IsInternal => false;

    /// <inheritdoc cref="ISlashCommandHandler.IsEnabled"/>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc cref="ISlashCommandHandler.Options"/>
    public SlashCommandOptionBuilder[] Options => new[]
    {
        new SlashCommandOptionBuilder()
            .WithName("roblox_id")
            .WithDescription("Render a user by their Roblox User ID.")
            .WithType(ApplicationCommandOptionType.SubCommand)
            .AddOption("id", ApplicationCommandOptionType.Integer, "The user ID of the Roblox user.", true, minValue: 1),

        new SlashCommandOptionBuilder()
            .WithName("roblox_name")
            .WithDescription("Render a user by their Roblox Username.")
            .WithType(ApplicationCommandOptionType.SubCommand)
            .AddOption("user_name", ApplicationCommandOptionType.String, "The user name of the Roblox user.", true)
    };

    // language=regex
    private const string _goodUsernameRegex = @"^[A-Za-z0-9_]{3,20}$";

    private static async Task<long> GetUserIdAsync(
        SocketSlashCommand item,
        SocketSlashCommandDataOption subCommand
    )
    {
        var userId = 0L;

        switch (subCommand.Name.ToLower())
        {
            case "roblox_id":
                userId = subCommand.GetOptionValue<long>("id");
                break;
            case "roblox_name":
                var username = subCommand.GetOptionValue<string>("user_name")?.EscapeNewLines()?.Escape();

                if (username.IsNullOrEmpty())
                {
                    Logger.Singleton.Warning("The user's input username was null or empty, they clearly do not know how to input text.");
                    await item.RespondEphemeralPingAsync($"Missing required parameter 'user_name'.");

                    return long.MinValue;
                }

                if (!username.IsMatch(_goodUsernameRegex))
                {
                    Logger.Singleton.Warning("Invalid username '{0}'", username);
                    await item.RespondEphemeralPingAsync("The username you presented contains invalid charcters!");

                    return long.MinValue;
                }

                Logger.Singleton.Debug("Trying to get the ID of the user by this username '{0}'", username);

                if (AvatarSettings.Singleton.BlacklistedUsernamesForRendering.Contains(username))
                {
                    await item.RespondEphemeralPingAsync($"The username '{username}' is a blacklisted username, please try again later.");

                    return long.MinValue;
                }

                var nullableUserIdRemote = RbxUsersUtility.GetUserIdByUsername(username);
                if (!nullableUserIdRemote.HasValue)
                {
                    Logger.Singleton.Warning("The ID for the user '{0}' was null, they were either banned or do not exist.", username);
                    await item.RespondEphemeralPingAsync($"The user by the username of '{username}' was not found.");

                    return long.MinValue;
                }

                Logger.Singleton.Information("The ID for the user '{0}' was {1}.", username, nullableUserIdRemote.Value);
                userId = nullableUserIdRemote.Value;

                break;
        }

        return userId;
    }

    /// <inheritdoc cref="ISlashCommandHandler.ExecuteAsync(SocketSlashCommand)"/>
    public async Task ExecuteAsync(SocketSlashCommand command)
    {
        var userIsAdmin = command.User.IsAdmin();

        if (FloodCheckerRegistry.RenderFloodChecker.IsFlooded() && !userIsAdmin) // allow admins to bypass
        {
            await command.RespondEphemeralAsync("Too many people are using this command at once, please wait a few moments and try again.");

            return;
        }

        FloodCheckerRegistry.RenderFloodChecker.UpdateCount();

        var perUserFloodChecker = FloodCheckerRegistry.GetPerUserRenderFloodChecker(command.User.Id);
        if (perUserFloodChecker.IsFlooded() && !userIsAdmin)
        {
            await command.RespondEphemeralAsync("You are sending render commands too quickly, please wait a few moments and try again.");

            return;
        }

        perUserFloodChecker.UpdateCount();

        var subCommand = command.Data.GetSubCommand();

        var userId = await GetUserIdAsync(command, subCommand);
        if (userId == long.MinValue) return;

        if (RbxUsersUtility.GetIsUserBanned(userId))
        {
            Logger.Singleton.Warning("The input user ID of {0} was linked to a banned user account.", userId);
            await command.RespondEphemeralPingAsync($"The user '{userId}' is banned or does not exist.");

            return;
        }

        Logger.Singleton.Information(
            "Trying to render the character for the user '{0}' with the place '{1}', " +
            "and the dimensions of {2}x{3}",
            userId,
            AvatarSettings.Singleton.PlaceIdForRenders,
            AvatarSettings.Singleton.RenderXDimension,
            AvatarSettings.Singleton.RenderYDimension
        );

        // get a stream and temp filename
        var (stream, fileName) = AvatarUtility.RenderUser(
            userId,
            AvatarSettings.Singleton.PlaceIdForRenders,
            AvatarSettings.Singleton.RenderXDimension,
            AvatarSettings.Singleton.RenderYDimension
        );

        if (stream == null || fileName == null)
        {
            await command.RespondEphemeralPingAsync("Internal failure when processing item render.");

            return;
        }

        using (stream)
            await command.RespondWithFilePublicPingAsync(
                stream,
                fileName
            );
    }
}

#endif
