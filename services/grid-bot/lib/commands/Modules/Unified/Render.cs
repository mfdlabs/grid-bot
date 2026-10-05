namespace Grid.Bot.UnifiedCommands.Public;

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Discord;
using Discord.Commands;
using Discord.Interactions;

using Prometheus;

using Random;
using Logging;
using FileSystem;
using Thumbnails.Client;
using Threading.Extensions;

using Grid.Commands;
using Grid.ProcessManagement.Core;

using Utility;
using Commands;

using GridJob = Grid.Client.Job;
using ThumbnailFormat = Thumbnails.Client.Format;

using TextCommandSummary = Discord.Commands.SummaryAttribute;
using InteractionGroup = Discord.Interactions.GroupAttribute;
using InteractionSummary = Discord.Interactions.SummaryAttribute;

using TextCommandModuleBase = Discord.Commands.ModuleBase;
using InteractionModuleBase = Discord.Interactions.InteractionModuleBase;

/// <summary>
/// Exception thrown when rbx-thumbnails returns a state that is not pending or completed.
/// </summary>
/// <remarks>
/// Construct a new instance of <see cref="ThumbnailResponseException"/>.
/// </remarks>
/// <param name="state">The <see cref="ThumbnailResponseState"/>.</param>
/// <param name="message">The message.</param>
/// <param name="innerException">The inner <see cref="Exception"/>.</param>
public class ThumbnailResponseException(ThumbnailResponseState state, string message, Exception innerException = null) : Exception(message, innerException)
{
    /// <summary>
    /// Gets the <see cref="ThumbnailResponseState"/>.
    /// </summary>
    public ThumbnailResponseState State { get; } = state;

    /// <inheritdoc cref="Exception.Message"/>
    public override string Message => $"The thumbnail response state was '{State}' and the message was '{base.Message}'";
}

/// <summary>
/// Provides functionality for rendering avatars and managing related metrics.
/// </summary>
public class Render
{

    #region Metrics

    private static readonly Gauge _avatarThumbnailsRbxThumbnailsRolloutPercent = Metrics.CreateGauge(
        "avatar_thumbnails_rbx_thumbnails_rollout_percent",
        "The percentage of users that are using the rbx-thumbnails rollout."
    );
    private static readonly Gauge _avatarThumbnailsIdsNotToUseTotal = Metrics.CreateGauge(
        "avatar_thumbnails_ids_not_to_use_total",
        "The total number of IDs that are not to be used."
    );
    private static readonly Gauge _avatarThumbnailsLocalCacheSize = Metrics.CreateGauge(
        "avatar_thumbnails_local_cache_size",
        "The size of the local cache."
    );

    private static readonly Counter _avatarThumbnailsRbxThumbnailsStatusTotal = Metrics.CreateCounter(
        "avatar_thumbnails_rbx_thumbnails_status_total",
        "The total number of statuses returned from rbx-thumbnails.",
        "status"
    );
    private static readonly Counter _avatarThumbnailsRbxThumbnailsFetchTotal = Metrics.CreateCounter(
        "avatar_thumbnails_rbx_thumbnails_fetch_total",
        "The total number of fetches from rbx-thumbnails.",
        "user_id",
        "command_type"
    );
    private static readonly Counter _avatarThumbnailsRbxThumbnailsFetchBlacklistedIdUsedTotal = Metrics.CreateCounter(
        "avatar_thumbnails_rbx_thumbnails_fetch_blacklisted_id_used_total",
        "The total number of times a blacklisted ID was used.",
        "user_id"
    );
    private static readonly Counter _avatarThumbnailsRenderedTotal = Metrics.CreateCounter(
        "avatar_thumbnails_rendered_total",
        "The total number of rendered thumbnails.",
        "user_id",
        "command_type"
    );
    private static readonly Counter _avatarThumbnailsOptedInToRbxThumbnailsTotal = Metrics.CreateCounter(
        "avatar_thumbnails_via_rbx_thumbnails_total",
        "The total number of users that were rendered via rbx-thumbnails.",
        "user_id"
    );
    private static readonly Counter _avatarThumbnailsOptedOutOfRbxThumbnailsTotal = Metrics.CreateCounter(
        "avatar_thumbnails_via_grid_server_total",
        "The total number of users that were rendered via the old method.",
        "user_id",
        "place_id",
        "command_type",
        "dimension_x",
        "dimension_y"
    );
    private static readonly Counter _avatarThumbnailsLegacyRejectedTotal = Metrics.CreateCounter(
        "avatar_thumbnails_legacy_rejected_total",
        "The total number of legacy rejections.",
        "user_id",
        "rejection_reason"
    );
    private static readonly Counter _avatarThumbnailsLegacyErrorTotal = Metrics.CreateCounter(
        "avatar_thumbnails_legacy_error_total",
        "The total number of legacy errors.",
        "user_id"
    );

    #endregion Metrics

    #region Dependencies

    private readonly AvatarSettings _avatarSettings;
    private readonly ILogger _logger;
    private readonly IRandom _random;
    private readonly IJobManager _jobManager;
    private readonly IThumbnailsClient _thumbnailsClient;
    private readonly IPercentageInvoker _percentageInvoker;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IFloodCheckerRegistry _floodCheckerRegistry;
    private readonly IAdminUtility _adminUtility;

    #endregion Dependencies

    private readonly ExpirableDictionary<(long, ThumbnailCommandType), string> _localCachedPaths;
    private readonly ConcurrentBag<long> _idsNotToUse = []; // These IDs error out when trying to render (they are blocked? or they are moderated?)

    /// <summary>
    /// Construct a new instance of <see cref="Render"/>.
    /// </summary>
    /// <param name="avatarSettings">The <see cref="AvatarSettings"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="random">The <see cref="IRandom"/>.</param>
    /// <param name="jobManager">The <see cref="IJobManager"/>.</param>
    /// <param name="thumbnailsClient">The <see cref="IThumbnailsClient"/>.</param>
    /// <param name="percentageInvoker">The <see cref="IPercentageInvoker"/>.</param>
    /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/>.</param>
    /// <param name="floodCheckerRegistry">The <see cref="IFloodCheckerRegistry"/>.</param>
    /// <param name="adminUtility">The <see cref="IAdminUtility"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="avatarSettings"/> cannot be null.
    /// - <paramref name="logger"/> cannot be null.
    /// - <paramref name="random"/> cannot be null.
    /// - <paramref name="jobManager"/> cannot be null.
    /// - <paramref name="thumbnailsClient"/> cannot be null.
    /// - <paramref name="percentageInvoker"/> cannot be null.
    /// - <paramref name="httpClientFactory"/> cannot be null.
    /// - <paramref name="floodCheckerRegistry"/> cannot be null.
    /// - <paramref name="adminUtility"/> cannot be null.
    /// </exception>
    public Render(
        AvatarSettings avatarSettings,
        ILogger logger,
        IRandom random,
        IJobManager jobManager,
        IThumbnailsClient thumbnailsClient,
        IPercentageInvoker percentageInvoker,
        IHttpClientFactory httpClientFactory,
        IFloodCheckerRegistry floodCheckerRegistry,
        IAdminUtility adminUtility
    )
    {
        _avatarSettings = avatarSettings ?? throw new ArgumentNullException(nameof(avatarSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _random = random ?? throw new ArgumentNullException(nameof(random));
        _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
        _thumbnailsClient = thumbnailsClient ?? throw new ArgumentNullException(nameof(thumbnailsClient));
        _percentageInvoker = percentageInvoker ?? throw new ArgumentNullException(nameof(percentageInvoker));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _floodCheckerRegistry = floodCheckerRegistry ?? throw new ArgumentNullException(nameof(floodCheckerRegistry));
        _adminUtility = adminUtility ?? throw new ArgumentNullException(nameof(adminUtility));

        if (!Uri.TryCreate(
                _avatarSettings.UsersApiUrl + _multiGetUsersByUsernamesEndpoint,
                UriKind.Absolute, out var multiGetUsersByUsernamesUri))
            throw new InvalidOperationException("Invalid Users API URL for multi-get users by usernames endpoint.");

        if (!Uri.TryCreate(
                _avatarSettings.UsersApiUrl + _multiGetUsersByIdsEndpoint,
                UriKind.Absolute, out var multiGetUsersByIdsUri))
            throw new InvalidOperationException("Invalid Users API URL for multi-get users by IDs endpoint.");

        _multiGetUsersByIdsFullUrl = multiGetUsersByIdsUri;
        _multiGetUsersByUsernamesFullUrl = multiGetUsersByUsernamesUri;

        _localCachedPaths = new(avatarSettings.LocalCacheTtl);
        _localCachedPaths.EntryRemoved += OnLocalCacheEntryRemoved;

        foreach (var id in avatarSettings.BlacklistUserIds)
            _idsNotToUse.Add(id);

        _avatarThumbnailsRbxThumbnailsRolloutPercent.Set(_avatarSettings.RbxThumbnailsRolloutPercent);
        _avatarThumbnailsIdsNotToUseTotal.Set(_idsNotToUse.Count);

        if (!_idsNotToUse.IsEmpty)
            _logger.Warning("Blacklisted user IDs: {0}", string.Join(", ", avatarSettings.BlacklistUserIds));

        Task.Factory.StartNew(PersistBlacklistedIdsThread, TaskCreationOptions.LongRunning);
    }

    #region Blacklisted IDs Runner

    private void PersistBlacklistedIdsThread()
    {
        while (true)
        {
            Task.Delay(_avatarSettings.BlacklistPersistPeriod).Wait();

            if (_avatarSettings.BlacklistUserIds.SequenceEqual(_idsNotToUse))
                continue;

            // Logging purposes: grab any new ones that were added.
            var newIds = _idsNotToUse.Except(_avatarSettings.BlacklistUserIds).ToArray();
            var removedIds = _avatarSettings.BlacklistUserIds.Except(_idsNotToUse).ToArray();

            _logger.Warning(
                "Blacklisted user IDs were updated. New IDs: {0}, Removed IDs: {1}",
                string.Join(", ", newIds),
                string.Join(", ", removedIds)
            );

            _avatarSettings.BlacklistUserIds = [.. _idsNotToUse];

            _avatarThumbnailsIdsNotToUseTotal.Inc();
        }
    }

    #endregion

    #region rbx-thumbnails

    private void OnLocalCacheEntryRemoved(string path, RemovalReason reason)
    {
        _avatarThumbnailsLocalCacheSize.Dec();

        if (reason == RemovalReason.Expired)
        {
            _logger.Warning("The local cache entry '{0}' expired.", path);

            try
            {
                path.PollDeletionBlocking();
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }
    }


    private async Task<string> PollThumbnailRequestUntilCompleteAsync(long userId, Func<Task<ThumbnailResponse>> func)
    {
        var response = await func().ConfigureAwait(false) ?? throw new ThumbnailResponseException(ThumbnailResponseState.Error, "The thumbnail response was null.");

        _avatarThumbnailsRbxThumbnailsStatusTotal.WithLabels(response.State.ToString()).Inc();

        if (response.State == ThumbnailResponseState.Completed)
            return response.ImageUrl;

        if (response.State != ThumbnailResponseState.Pending)
        {
            if (response.State != ThumbnailResponseState.InReview)
            {
                _idsNotToUse.Add(userId);

                _avatarThumbnailsIdsNotToUseTotal.Inc();
            }

            throw new ThumbnailResponseException(response.State.GetValueOrDefault(), "The thumbnail response was not pending.");
        }

        while (response.State == ThumbnailResponseState.Pending)
        {
            Task.Delay(1000).Wait();

            response = await func().ConfigureAwait(false);
        }

        if (response.State != ThumbnailResponseState.Completed)
        {
            if (response.State != ThumbnailResponseState.InReview)
            {
                _idsNotToUse.Add(userId);

                _avatarThumbnailsIdsNotToUseTotal.Inc();
            }

            throw new ThumbnailResponseException(response.State.GetValueOrDefault(), "The thumbnail response was not completed.");
        }

        return response.ImageUrl;
    }

    private async Task<string> DownloadThumbnailFileAsync(string url)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");
        var file = File.OpenWrite(path);

        using var client = _httpClientFactory.CreateClient("rbx-thumbnails");
        using var stream = await client.GetStreamAsync(url).ConfigureAwait(false) ?? throw new ThumbnailResponseException(ThumbnailResponseState.Error, "The thumbnail response stream was null");
        stream.CopyTo(file);

        file.Close();
        file.Dispose();

        return path;
    }

    private async Task<string> DoFetchRbxThumbnail(long userId, ThumbnailCommandType thumbnailCommandType)
    {
        _avatarThumbnailsLocalCacheSize.Inc();

        _logger.Warning(
            "Entry for user '{0}' with the thumbnail command type of '{1}' was not found in the local cache, " +
            "fetching from rbx-thumbnails and caching locally.",
            userId,
            thumbnailCommandType
        );

        string url = null;

        switch (thumbnailCommandType)
        {
            case ThumbnailCommandType.Closeup:
                url = await PollThumbnailRequestUntilCompleteAsync(
                    userId,
                    async () => (await _thumbnailsClient.GetAvatarHeadshotThumbnailAsync(
                        [userId],
                        _avatarSettings.RenderDimensions,
                        ThumbnailFormat.Png,
                        false
                    ).ConfigureAwait(false))?.Data?.FirstOrDefault()
                ).ConfigureAwait(false);

                return await DownloadThumbnailFileAsync(url).ConfigureAwait(false);
            case ThumbnailCommandType.Avatar_R15_Action:
                url = await PollThumbnailRequestUntilCompleteAsync(
                    userId,
                    async () => (await _thumbnailsClient.GetAvatarThumbnailAsync(
                        [userId],
                        _avatarSettings.RenderDimensions,
                        ThumbnailFormat.Png,
                        false
                    ).ConfigureAwait(false))?.Data?.FirstOrDefault()
                ).ConfigureAwait(false);

                return await DownloadThumbnailFileAsync(url).ConfigureAwait(false);
            default:
                throw new ArgumentOutOfRangeException(nameof(thumbnailCommandType), thumbnailCommandType, null);
        }
    }

    private (Stream, string) DoRenderByRbxThumbnails(long userId, ThumbnailCommandType thumbnailCommandType)
    {
        _avatarThumbnailsOptedInToRbxThumbnailsTotal.WithLabels(userId.ToString()).Inc();

        _logger.Warning(
            "Trying to fetch the thumbnail for user '{0}' via rbx-thumbnails with the dimensions of {1}",
            userId,
            _avatarSettings.RenderDimensions
        );

        _avatarThumbnailsRbxThumbnailsFetchTotal.WithLabels(userId.ToString(), thumbnailCommandType.ToString()).Inc();

        if (userId == 0)
            throw new ArgumentException("The user ID cannot be 0.", nameof(userId));

        if (_idsNotToUse.Contains(userId))
        {
            _avatarThumbnailsRbxThumbnailsFetchBlacklistedIdUsedTotal.WithLabels(userId.ToString()).Inc();

            throw new ThumbnailResponseException(ThumbnailResponseState.Blocked, "The user ID is blacklisted.");
        }

        if (thumbnailCommandType != ThumbnailCommandType.Closeup && thumbnailCommandType != ThumbnailCommandType.Avatar_R15_Action)
            throw new ArgumentException("The thumbnail command type must be either closeup or avatar_r15_action.", nameof(thumbnailCommandType));

        var userIds = new[] { userId };

        var path = _localCachedPaths.GetOrAdd(
            (userId, thumbnailCommandType),
            _ => DoFetchRbxThumbnail(userId, thumbnailCommandType).Sync()
        );

        using var file = File.OpenRead(path);
        var memoryStream = new MemoryStream();

        file.CopyTo(memoryStream);

        return (memoryStream, Path.GetFileName(path));
    }

    #endregion

    #region RCC renders

    private IEnumerable<object> GetThumbnailCommandArguments(string url, int x, int y)
    {
        yield return _avatarSettings.RenderAssetFetchUrl; // baseUrl
        yield return url; // characterAppearanceUrl
        yield return _avatarSettings.RenderThumbnailType; // fileExtension 
        yield return x; // x
        yield return y; // y

        // these are specific to closeups.
        yield return true; // quadratic
        yield return 30; // baseHatZoom
        yield return 100; // maxHatZoom
        yield return 0; // cameraOffsetX
        yield return 0; // cameraOffsetY
    }

    private string GetAvatarFetchUrl(long userId, long placeId)
        => $"{_avatarSettings.AvatarFetchUrl}?userId={userId}&placeId={placeId}";

    private string GetFileNameForRccThumbnail(long userId, long placeId, ThumbnailSettings settings)
    {
        var args = settings.Arguments;

        return $"{Guid.NewGuid()}_" +
               $"{userId}_" +
               $"{placeId}_" +
               $"{settings.Type}_" +
               $"{args[2]}_" +
               $"{args[3]}_" +
               $"{args[4]}_" +
               $"{args[5]}_" +
               $"{args[6]}_" +
               $"{args[7]}_" +
               $"{args[8]}_" +
               $"{args[9]}.{_avatarSettings.RenderThumbnailType.ToLower()}";
    }

    private async Task<(Stream, string)> DoRenderByRccAsync(
        long userId,
        long placeId,
        ThumbnailCommandType thumbnailCommandType,
        int sizeX,
        int sizeY
    )
    {
        var url = GetAvatarFetchUrl(userId, placeId);

        _logger.Warning(
            "Trying to render user '{0}' in place '{1}' with the dimensions of {2}x{3} with the url '{4}'",
            userId,
            placeId,
            sizeX,
            sizeY,
            url
        );

        _avatarThumbnailsOptedOutOfRbxThumbnailsTotal.WithLabels(userId.ToString(), placeId.ToString(), thumbnailCommandType.ToString(), sizeX.ToString(), sizeY.ToString()).Inc();

        var settings = new ThumbnailSettings(thumbnailCommandType, [.. GetThumbnailCommandArguments(url, sizeX, sizeY)]);

#if !PRE_JSON_EXECUTION
        var renderScript = new ThumbnailCommand(settings);
#else
        var renderScript = Lua.NewScript(
            commandType.ToString(),
            ScriptProvider.GetScript(thumbnailCommandType),
            GetThumbnailArgs(url, sizeX, sizeY).ToArray()
        );
#endif

        var job = new Job(Guid.NewGuid().ToString());

        try
        {
            var (soap, _, rejectionReason) = _jobManager.NewJob(job, _avatarSettings.RenderJobTimeout.TotalSeconds, true);

            if (rejectionReason != null)
            {
                _avatarThumbnailsLegacyRejectedTotal.WithLabels(userId.ToString(), rejectionReason.ToString()).Inc();

                _logger.Error("The job was rejected: {0}", rejectionReason);

                return (null, null);
            }

            using (soap)
            {

                var result = await soap.BatchJobExAsync(
                    new GridJob()
                    {
                        id = Guid.NewGuid().ToString(),
                        expirationInSeconds = _avatarSettings.RenderJobTimeout.TotalSeconds
                    },
                    renderScript
                ).ConfigureAwait(false);

                Task.Run(() => _jobManager.CloseJob(job, true));

                var first = result.ElementAt(0);
                if (first != null)
                    return (new MemoryStream(Convert.FromBase64String(first.value)), GetFileNameForRccThumbnail(userId, placeId, settings));

                _avatarThumbnailsLegacyErrorTotal.WithLabels(userId.ToString()).Inc();

                _logger.Error("The first return argument for the render was null, this may be an issue with the grid server.");

                return (null, null);
            }
        }
        catch (Exception ex)
        {
            Task.Run(() => _jobManager.CloseJob(job, false));
            _logger.Error(ex);
            _avatarThumbnailsLegacyErrorTotal.WithLabels(userId.ToString()).Inc();

            return (null, null);
        }
    }

    #endregion

    #region rbx-users

    private const string _multiGetUsersByUsernamesEndpoint = "/v1/usernames/users";
    private readonly Uri _multiGetUsersByUsernamesFullUrl;

    private async Task<long?> ResolveUserIdAsync(string username)
    {
        var request = new
        {
            excludeBannedUsers = false,
            usernames = new string[1] { username }
        };

        try
        {
            using var httpClient = _httpClientFactory.CreateClient();

            var response = await httpClient.PostAsJsonAsync(_multiGetUsersByUsernamesFullUrl, request);
            var responseData = await response.Content.ReadFromJsonAsync<JsonElement>();

            if (responseData.TryGetProperty("data", out var data) && data.GetArrayLength() > 0)
                return data[0].GetProperty("id").GetInt64();

            return null;
        }
        catch
        {
            return null;
        }
    }

    private const string _multiGetUsersByIdsEndpoint = "/v1/users";
    private readonly Uri _multiGetUsersByIdsFullUrl;

    private async Task<bool> DetermineIfUserExistsAsync(long id)
    {

        var request = new
        {
            excludeBannedUsers = false,
            userIds = new long[1] { id }
        };

        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.PostAsJsonAsync(_multiGetUsersByIdsFullUrl, request);
            var responseData = await response.Content.ReadFromJsonAsync<JsonElement>();

            return responseData.TryGetProperty("data", out var data) && data.GetArrayLength() > 0;
        }
        catch
        {
            return false;
        }
    }

    #endregion

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
    public async Task RenderAsync(IUnifiedCommandContext context, string userNameOrId)
    {
        RenderPerformanceCounters.TotalRenders.WithLabels(userNameOrId).Inc();

        if (!long.TryParse(userNameOrId, out var userId))
        {
            RenderPerformanceCounters.TotalRendersViaUsername.Inc();

            var id = await ResolveUserIdAsync(userNameOrId).ConfigureAwait(false);
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

        if (!await DetermineIfUserExistsAsync(userId).ConfigureAwait(false))
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
            var thumbnailCommandType = _random.Next(0, 10) < 6
            ? ThumbnailCommandType.Avatar_R15_Action
            : ThumbnailCommandType.Closeup;

            _avatarThumbnailsRenderedTotal.WithLabels(userId.ToString(), thumbnailCommandType.ToString()).Inc();

            var (stream, fileName) = _percentageInvoker.CanInvoke(_avatarSettings.RbxThumbnailsRolloutPercent)
                ? DoRenderByRbxThumbnails(
                    userId,
                    thumbnailCommandType
                )
                : await DoRenderByRccAsync(
                    userId,
                    _avatarSettings.PlaceIdForRenders,
                    thumbnailCommandType,
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
    public async Task RenderAsync(string userNameOrId)
        => await _renderCommand.RenderAsync(new UnifiedCommandContext(Context), userNameOrId).ConfigureAwait(false);
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
    ) => await _renderCommand.RenderAsync(new UnifiedCommandContext(Context), id.ToString()).ConfigureAwait(false);

    [SlashCommand("username", "Renders a Roblox character by Roblox username.")]
    public async Task RenderByUsernameAsync(
        [InteractionSummary("username", "The username of the Roblox user.")]
        string username
    ) => await _renderCommand.RenderAsync(new UnifiedCommandContext(Context), username).ConfigureAwait(false);
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

#endregion