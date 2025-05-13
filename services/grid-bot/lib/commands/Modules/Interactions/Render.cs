namespace Grid.Bot.Interactions.Public;

using System;
using System.Threading.Tasks;

using Discord;
using Discord.Interactions;

using Logging;
using Thumbnails.Client;

using Utility;

/// <summary>
/// Interaction handler for rendering a Roblox character.
/// </summary>
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
[Group("render", "Commands used for rendering a Roblox character.")]
[IntegrationType(ApplicationIntegrationType.GuildInstall, ApplicationIntegrationType.UserInstall)]
[CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
public class Render(
    AvatarSettings avatarSettings,
    ILogger logger,
    IRbxUsersUtility rbxUsersUtility,
    IAvatarUtility avatarUtility,
    IFloodCheckerRegistry floodCheckerRegistry,
    IAdminUtility adminUtility
) : InteractionModuleBase<ShardedInteractionContext>
{
    private readonly AvatarSettings _avatarSettings = avatarSettings ?? throw new ArgumentNullException(nameof(avatarSettings));

    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IRbxUsersUtility _rbxUsersUtility = rbxUsersUtility ?? throw new ArgumentNullException(nameof(rbxUsersUtility));
    private readonly IAvatarUtility _avatarUtility = avatarUtility ?? throw new ArgumentNullException(nameof(avatarUtility));
    private readonly IFloodCheckerRegistry _floodCheckerRegistry = floodCheckerRegistry ?? throw new ArgumentNullException(nameof(floodCheckerRegistry));
    private readonly IAdminUtility _adminUtility = adminUtility ?? throw new ArgumentNullException(nameof(adminUtility));


    /// <inheritdoc cref="InteractionModuleBase{TContext}.BeforeExecuteAsync(ICommandInfo)"/>
    public override async Task BeforeExecuteAsync(ICommandInfo command)
    {
        if (!_adminUtility.UserIsAdmin(Context.User))
        {
            if (_floodCheckerRegistry.RenderFloodChecker.IsFlooded())
            {
                RenderPerformanceCounters.TotalRendersBlockedByGlobalFloodChecker.Inc();

                throw new ApplicationException("Too many people are using this command at once, please wait a few moments and try again.");
            }

            _floodCheckerRegistry.RenderFloodChecker.UpdateCount();

            var perUserFloodChecker = _floodCheckerRegistry.GetPerUserRenderFloodChecker(Context.User.Id);
            if (perUserFloodChecker.IsFlooded())
            {
                RenderPerformanceCounters.TotalRendersBlockedByPerUserFloodChecker.WithLabels(Context.User.Id.ToString()).Inc();

                throw new ApplicationException("You are sending render commands too quickly, please wait a few moments and try again.");
            }

            perUserFloodChecker.UpdateCount();
        }

        await base.BeforeExecuteAsync(command);
    }

    /// <summary>
    /// Renders a Roblox character by Roblox user ID.
    /// </summary>
    /// <param name="id">The ID of the Roblox user.</param>
    [SlashCommand("id", "Renders a Roblox character by Roblox user ID.")]
    public async Task RenderByIdAsync(
        [Summary("id", "The ID of the Roblox user.")]
        long id
    )
    {
        RenderPerformanceCounters.TotalRenders.WithLabels(id.ToString()).Inc();

        if (id < 1)
        {
            RenderPerformanceCounters.TotalRendersWithInvalidIds.Inc();

            await FollowupAsync("The ID must be greater than 0.");

            return;
        }

        if (await _rbxUsersUtility.GetIsUserBannedAsync(id).ConfigureAwait(false))
        {
            RenderPerformanceCounters.TotalRendersAgainstBannedUsers.Inc();

            _logger.Warning("The input user ID of {0} was linked to a banned user account.", id);
            await FollowupAsync($"The user '{id}' is banned or does not exist.");

            return;
        }

        _logger.Information(
            "Trying to render the character for the user '{0}' with the place '{1}', " +
            "and the dimensions of {2}x{3}",
            id,
            _avatarSettings.PlaceIdForRenders,
            _avatarSettings.RenderXDimension,
            _avatarSettings.RenderYDimension
        );

        try
        {

            var (stream, fileName) = _avatarUtility.RenderUser(
                id,
                _avatarSettings.PlaceIdForRenders,
                _avatarSettings.RenderXDimension,
                _avatarSettings.RenderYDimension
            );

            if (stream == null)
            {
                await FollowupAsync("An error occurred while rendering the character.");

                return;
            }

            using (stream)
                await FollowupWithFileAsync(
                    stream,
                    fileName
                );

        }
        catch (ThumbnailResponseException e)
        {
            RenderPerformanceCounters.TotalRendersWithRbxThumbnailsErrors.WithLabels(e.State.ToString()).Inc();

            _logger.Warning("The thumbnail service responded with the following state: {0}, message: {1}", e.State, e.Message);

            if (e.State == ThumbnailResponseState.InReview)
            {
                // Bogus error here for the sake of the user. Like flood checker error.
                await FollowupAsync("The thumbnail service placed the request in review, please try again later.");

                return;
            }

            // Bogus error for anything else, we don't need this to be noted that we are using rbx-thumbnails.
            await FollowupAsync($"The thumbnail service responded with the following state: {e.State}");
        }
        catch (Exception e)
        {
            RenderPerformanceCounters.TotalRendersWithErrors.Inc();

            _logger.Error("An error occurred while rendering the character for the user '{0}': {1}", id, e);

            await FollowupAsync("An error occurred while rendering the character.");
        }
    }

    /// <summary>
    /// Renders a Roblox character by Roblox username.
    /// </summary>
    /// <param name="username">The username of the Roblox user.</param>
    [SlashCommand("username", "Renders a Roblox character by Roblox username.")]
    public async Task RenderByUsernameAsync(
        [Summary("username", "The username of the Roblox user.")]
        string username
    )
    {
        RenderPerformanceCounters.TotalRendersViaUsername.Inc();

        if (string.IsNullOrWhiteSpace(username))
        {
            await FollowupAsync("The username cannot be empty.");

            return;
        }

        var id = await _rbxUsersUtility.GetUserIdByUsernameAsync(username).ConfigureAwait(false);
        if (id == null)
        {
            await FollowupAsync($"The user '{username}' does not exist.");

            return;
        }

        await RenderByIdAsync(id.Value).ConfigureAwait(false);
    }
}
