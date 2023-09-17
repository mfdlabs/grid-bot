namespace Grid.Bot.Commands;

using System;
using System.Linq;
using System.Threading.Tasks;

using Discord.WebSocket;

using Logging;

using Text.Extensions;

using Utility;
using Interfaces;
using Extensions;

/// <summary>
/// Renders a Roblox character.
/// </summary>
[Obsolete("Text-based commands are being deprecated. Please migrate to using the /render slash command.")]
internal class Render : ICommandHandler
{
    /// <inheritdoc cref="ICommandHandler.Name"/>
    public string Name => "Render User";

    /// <inheritdoc cref="ICommandHandler.Description"/>
    public string Description => $"Renders a Roblox user!\nLayout: " +
                                        $"{CommandsSettings.Singleton.Prefix}render " +
                                        $"robloxUserID?|...userName?";

    /// <inheritdoc cref="ICommandHandler.Aliases"/>
    public string[] Aliases => new[] { "r", "render" };

    /// <inheritdoc cref="ICommandHandler.IsInternal"/>
    public bool IsInternal => false;

    /// <inheritdoc cref="ICommandHandler.IsEnabled"/>
    public bool IsEnabled { get; set; } = true;

    // language=regex
    private const string _goodUsernameRegex = @"^[A-Za-z0-9_]{3,20}$";

    /// <inheritdoc cref="ICommandHandler.ExecuteAsync(string[], SocketMessage, string)"/>
    public async Task ExecuteAsync(string[] contentArray, SocketMessage message, string originalCommandName)
    {
        using (message.Channel.EnterTypingState())
        {
            var userIsAdmin = message.Author.IsAdmin();

            if (FloodCheckerRegistry.RenderFloodChecker.IsFlooded() && !userIsAdmin) // allow admins to bypass
            {
                await message.ReplyAsync("Too many people are using this command at once, please wait a few moments and try again.");
                return;
            }

            FloodCheckerRegistry.RenderFloodChecker.UpdateCount();

            var perUserFloodChecker = FloodCheckerRegistry.GetPerUserRenderFloodChecker(message.Author.Id);
            if (perUserFloodChecker.IsFlooded() && !userIsAdmin)
            {
                await message.ReplyAsync("You are sending render commands too quickly, please wait a few moments and try again.");
                return;
            }

            perUserFloodChecker.UpdateCount();

            string username = null;

            if (!long.TryParse(contentArray.ElementAtOrDefault(0), out var userId))
            {
                Logger.Singleton.Warning("The first parameter of the command was " +
                                               "not a valid Int64, trying to get the userID " +
                                               "by username lookup.");
                username = contentArray.Join(' ').EscapeNewLines().Escape();

                if (AvatarSettings.Singleton.BlacklistedUsernamesForRendering.Contains(username))
                {
                    await message.ReplyAsync($"The username '{username}' is a blacklisted username, please try again later.");
                    return;
                }

                if (!username.IsNullOrEmpty())
                {
                    if (!username.IsMatch(_goodUsernameRegex))
                    {
                        Logger.Singleton.Warning("Invalid username '{0}'", username);

                        await message.ReplyAsync("The username you presented contains invalid charcters!");
                        return;
                    }

                    Logger.Singleton.Debug("Trying to get the ID of the user by this username '{0}'", username);
                    var nullableUserId = RbxUsersUtility.GetUserIdByUsername(username);

                    if (!nullableUserId.HasValue)
                    {
                        Logger.Singleton.Warning("The ID for the user '{0}' was null, they were either banned or do not exist.", username);
                        await message.ReplyAsync($"The user by the username of '{username}' was not found.");
                        return;
                    }

                    Logger.Singleton.Information("The ID for the user '{0}' was {1}.", username, nullableUserId.Value);
                    userId = nullableUserId.Value;
                }
                else
                {
                    Logger.Singleton.Warning("The user's input username was null or empty, they clearly do not know how to input text.");
                    await message.ReplyAsync($"Missing required parameter 'userID' or 'userName', " +
                                  $"the layout is: " +
                                  $"{CommandsSettings.Singleton.Prefix}{originalCommandName} " +
                                  $"userID|userName");
                    return;
                }
            }

            if (RbxUsersUtility.GetIsUserBanned(userId))
            {
                Logger.Singleton.Warning("The input user ID of {0} was linked to a banned user account.", userId);

                var user = userId == default 
                    ? username 
                    : userId.ToString();

                await message.ReplyAsync($"The user '{user}' is banned or does not exist.");

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
                await message.ReplyAsync("Internal failure when processing item render.");

                return;
            }

            using (stream)
                await message.ReplyWithFileAsync(
                    stream,
                    fileName
                );
        }
    }
}
