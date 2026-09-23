namespace Grid.Bot.UnifiedCommands.Public;

using System;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.Interactions;

using Logging;
using Thumbnails.Client;

using Utility;
using Commands;

using TextCommandSummary = Discord.Commands.SummaryAttribute;
using InteractionGroup = Discord.Interactions.GroupAttribute;
using InteractionSummary = Discord.Interactions.SummaryAttribute;

using TextCommandModuleBase = Discord.Commands.ModuleBase;
using InteractionModuleBase = Discord.Interactions.InteractionModuleBase;

/// <remarks>
/// Construct a new instance of <see cref="Render"/>.
/// </remarks>
/// <param name="avatarSettings">The <see cref="AvatarSettings"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
/// <param name="rbxUsersUtility">The <see cref="IRbxUsersUtility"/>.</param>
/// <param name="avatarUtility">The <see cref="IAvatarUtility"/>.</param>
/// <param name="floodCheckerRegistry">The <see cref="IFloodCheckerRegistry"/>.</param>
/// <param name="adminUtility">The <see cref="IAdminUtility"/>.</param>
/// <exception cref="ArgumentNullException">
/// - <paramref name="avatarSettings"/> cannot be null.
/// - <paramref name="logger"/> cannot be null.
/// - <paramref name="rbxUsersUtility"/> cannot be null.
/// - <paramref name="avatarUtility"/> cannot be null.
/// - <paramref name="floodCheckerRegistry"/> cannot be null.
/// - <paramref name="adminUtility"/> cannot be null.
/// </exception>
public class Render(
    AvatarSettings avatarSettings,
    ILogger logger,
    IRbxUsersUtility rbxUsersUtility,
    IAvatarUtility avatarUtility,
    IFloodCheckerRegistry floodCheckerRegistry,
    IAdminUtility adminUtility
)
{
    private readonly AvatarSettings _avatarSettings = avatarSettings ?? throw new ArgumentNullException(nameof(avatarSettings));

    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IRbxUsersUtility _rbxUsersUtility = rbxUsersUtility ?? throw new ArgumentNullException(nameof(rbxUsersUtility));
    private readonly IAvatarUtility _avatarUtility = avatarUtility ?? throw new ArgumentNullException(nameof(avatarUtility));
    private readonly IFloodCheckerRegistry _floodCheckerRegistry = floodCheckerRegistry ?? throw new ArgumentNullException(nameof(floodCheckerRegistry));
    private readonly IAdminUtility _adminUtility = adminUtility ?? throw new ArgumentNullException(nameof(adminUtility));

    /// <summary>
    /// Executes before the render command is processed, checking for admin status and flood control.
    /// </summary>
    /// <param name="context">The context of the unified command.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ApplicationException">
    /// - Thrown when the user is blocked by the global flood checker.
    /// - Thrown when the user is blocked by the per-user flood checker.
    /// </exception>
    public Task BeforeExecuteAsync(IUnifiedCommandContext context)
    {
        if (!_adminUtility.UserIsAdmin(context.User))
        {
            if (_floodCheckerRegistry.RenderFloodChecker.IsFlooded())
            {
                RenderPerformanceCounters.TotalRendersBlockedByGlobalFloodChecker.Inc();

                return Task.FromException(new ApplicationException("Too many people are using this command at once, please wait a few moments and try again."));
            }

            _floodCheckerRegistry.RenderFloodChecker.UpdateCount();

            var perUserFloodChecker = _floodCheckerRegistry.GetPerUserRenderFloodChecker(context.User.Id);
            if (perUserFloodChecker.IsFlooded())
            {
                RenderPerformanceCounters.TotalRendersBlockedByPerUserFloodChecker.WithLabels(context.User.Id.ToString()).Inc();

                return Task.FromException(new ApplicationException("You are sending render commands too quickly, please wait a few moments and try again."));
            }

            perUserFloodChecker.UpdateCount();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Renders a Roblox character by Roblox user ID.
    /// </summary>
    /// <param name="userNameOrId">The ID of the Roblox user.</param>
    /// <param name="context">The context of the unified command.</param>
    public async Task DoRenderAsync(string userNameOrId, IUnifiedCommandContext context)
    {
        RenderPerformanceCounters.TotalRenders.WithLabels(userNameOrId).Inc();

        if (!long.TryParse(userNameOrId, out var userId))
        {
            RenderPerformanceCounters.TotalRendersViaUsername.Inc();

            var id = await _rbxUsersUtility.GetUserIdByUsernameAsync(userNameOrId).ConfigureAwait(false);
            if (id == null)
            {
                await context.RespondAsync($"The user by the name '{userNameOrId}' does not exist.").ConfigureAwait(false);

                return;
            }

            userId = id.Value;
        }

        if (userId < 1)
        {
            RenderPerformanceCounters.TotalRendersWithInvalidIds.Inc();

            await context.RespondAsync("The ID must be greater than 0.").ConfigureAwait(false);

            return;
        }

        if (await _rbxUsersUtility.GetIsUserBannedAsync(userId).ConfigureAwait(false))
        {
            RenderPerformanceCounters.TotalRendersAgainstBannedUsers.WithLabels(userNameOrId).Inc();

            _logger.Warning("The input user ID of {0} was linked to a banned user account.", userId);
            await context.RespondAsync($"The user '{userNameOrId}' is banned or does not exist.").ConfigureAwait(false);

            return;
        }

        _logger.Information(
            "Trying to render the character for the user '{0}' with the place '{1}', " +
            "and the dimensions of {2}x{3}",
            userId,
            _avatarSettings.PlaceIdForRenders,
            _avatarSettings.RenderXDimension,
            _avatarSettings.RenderYDimension
        );

        try
        {

            var (stream, fileName) = _avatarUtility.RenderUser(
                userId,
                _avatarSettings.PlaceIdForRenders,
                _avatarSettings.RenderXDimension,
                _avatarSettings.RenderYDimension
            );

            if (stream == null)
            {
                RenderPerformanceCounters.TotalRendersWithErrors.Inc();

                await context.RespondAsync("An error occurred while rendering the character.").ConfigureAwait(false);

                return;
            }

            using (stream)
                await context.RespondWithFileAsync(
                    stream,
                    fileName
                ).ConfigureAwait(false);

        }
        catch (ThumbnailResponseException e)
        {
            RenderPerformanceCounters.TotalRendersWithRbxThumbnailsErrors.WithLabels(e.State.ToString()).Inc();

            _logger.Warning("The thumbnail service responded with the following state: {0}, message: {1}", e.State, e.Message);

            if (e.State == ThumbnailResponseState.InReview)
            {
                // Bogus error here for the sake of the user. Like flood checker error.
                await context.RespondAsync("The thumbnail service placed the request in review, please try again later.").ConfigureAwait(false);

                return;
            }

            // Bogus error for anything else, we don't need this to be noted that we are using rbx-thumbnails.
            await context.RespondAsync($"The thumbnail service responded with the following state: {e.State}").ConfigureAwait(false);
        }
        catch (Exception e)
        {
            RenderPerformanceCounters.TotalRendersWithErrors.Inc();

            _logger.Error("An error occurred while rendering the character for the user '{0}': {1}", userNameOrId, e);

            await context.RespondAsync("An error occurred while rendering the character.").ConfigureAwait(false);
        }
    }
}

#region MODULE PROXIES

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public class RenderTextCommand(Render renderCommand) : TextCommandModuleBase
{
    private readonly Render _renderCommand = renderCommand ?? throw new ArgumentNullException(nameof(renderCommand));

    protected override async Task BeforeExecuteAsync(CommandInfo command)
        => await _renderCommand.BeforeExecuteAsync(new UnifiedCommandContext(Context)).ConfigureAwait(false);


    [Command("render"), TextCommandSummary("Renders a Roblox character by Roblox user ID."), Alias("r")]
    public async Task DoRenderAsync(string userNameOrId)
        => await _renderCommand.DoRenderAsync(userNameOrId, new UnifiedCommandContext(Context)).ConfigureAwait(false);
}


[InteractionGroup("render", "Commands used for rendering a Roblox character.")]
[IntegrationType(ApplicationIntegrationType.GuildInstall, ApplicationIntegrationType.UserInstall)]
[CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
public class RenderInteraction(Render renderCommand) : InteractionModuleBase
{
    private readonly Render _renderCommand = renderCommand ?? throw new ArgumentNullException(nameof(renderCommand));

    public override async Task BeforeExecuteAsync(ICommandInfo command)
        => await _renderCommand.BeforeExecuteAsync(new UnifiedCommandContext(Context)).ConfigureAwait(false);

    [SlashCommand("id", "Renders a Roblox character by Roblox user ID.")]
    public async Task RenderByIdAsync(
        [InteractionSummary("id", "The ID of the Roblox user.")]
        long id
    ) => await _renderCommand.DoRenderAsync(id.ToString(), new UnifiedCommandContext(Context)).ConfigureAwait(false);

    [SlashCommand("username", "Renders a Roblox character by Roblox username.")]
    public async Task RenderByUsernameAsync(
        [InteractionSummary("username", "The username of the Roblox user.")]
        string username
    ) => await _renderCommand.DoRenderAsync(username, new UnifiedCommandContext(Context)).ConfigureAwait(false);
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

#endregion