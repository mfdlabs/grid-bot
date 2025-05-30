﻿namespace Grid.Bot.Utility;

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Prometheus;

using Random;
using Logging;
using FileSystem;
using Thumbnails.Client;
using Threading.Extensions;

using Grid.Commands;

using GridJob = Grid.Client.Job;

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

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBeMadeStatic.Global

/// <summary>
/// Utility to be used when interacting with the rendering
/// layer of the grid servers.
/// </summary>
public class AvatarUtility : IAvatarUtility
{
    private readonly ILogger _logger;
    private readonly AvatarSettings _avatarSettings;
    private readonly IRandom _random;
    private readonly IJobManager _jobManager;
    private readonly IThumbnailsClient _thumbnailsClient;
    private readonly IPercentageInvoker _percentageInvoker;
    private readonly IHttpClientFactory _httpClientFactory;

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
        "place_id",
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

    private readonly ExpirableDictionary<(long, ThumbnailCommandType), string> _localCachedPaths;
    private readonly ConcurrentBag<long> _idsNotToUse = []; // These IDs error out when trying to render (they are blocked? or they are moderated?)

    /// <summary>
    /// Construct a new instance of <see cref="AvatarUtility"/>.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="avatarSettings">The <see cref="AvatarSettings"/>.</param>
    /// <param name="random">The <see cref="IRandom"/>.</param>
    /// <param name="jobManager">The <see cref="IJobManager"/>.</param>
    /// <param name="thumbnailsClient">The <see cref="IThumbnailsClient"/>.</param>
    /// <param name="percentageInvoker">The <see cref="IPercentageInvoker"/>.</param>
    /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/>.</param>
    public AvatarUtility(
        ILogger logger,
        AvatarSettings avatarSettings,
        IRandom random,
        IJobManager jobManager,
        IThumbnailsClient thumbnailsClient,
        IPercentageInvoker percentageInvoker,
        IHttpClientFactory httpClientFactory
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _avatarSettings = avatarSettings ?? throw new ArgumentNullException(nameof(avatarSettings));
        _random = random ?? throw new ArgumentNullException(nameof(random));
        _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
        _thumbnailsClient = thumbnailsClient ?? throw new ArgumentNullException(nameof(thumbnailsClient));
        _percentageInvoker = percentageInvoker ?? throw new ArgumentNullException(nameof(percentageInvoker));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

        _localCachedPaths = new(avatarSettings.LocalCacheTtl);
        _localCachedPaths.EntryRemoved += OnLocalCacheEntryRemoved;

        foreach (var id in avatarSettings.BlacklistUserIds)
            _idsNotToUse.Add(id);

        _avatarThumbnailsRbxThumbnailsRolloutPercent.Set(_avatarSettings.RbxThumbnailsRolloutPercent);
        _avatarThumbnailsIdsNotToUseTotal.Set(_idsNotToUse.Count);

        if (!_idsNotToUse.IsEmpty)
            _logger.Warning("Blacklisted user IDs: {0}", string.Join(", ", avatarSettings.BlacklistUserIds));

        Task.Factory.StartNew(PersistBlacklistedIds, TaskCreationOptions.LongRunning);
    }

    private void PersistBlacklistedIds()
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

    private IEnumerable<object> GetThumbnailArgs(string url, int x, int y)
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

    private string PollUntilCompleted(long userId, Func<ThumbnailResponse> func)
    {
        var response = func() ?? throw new ThumbnailResponseException(ThumbnailResponseState.Error, "The thumbnail response was null.");

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

            response = func();
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

    private string DownloadFile(string url)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");
        var file = File.OpenWrite(path);

        using var client = _httpClientFactory.CreateClient("rbx-thumbnails");
        using var stream = client.GetStreamAsync(url).SyncOrDefault() ?? throw new ThumbnailResponseException(ThumbnailResponseState.Error, "The thumbnail response stream was null");
        stream.CopyTo(file);

        file.Close();
        file.Dispose();

        return path;
    }

    private (Stream, string) GetThumbnail(long userId, ThumbnailCommandType thumbnailCommandType)
    {
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
            (key) =>
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
                        url = PollUntilCompleted(
                            userId,
                            () => _thumbnailsClient.GetAvatarHeadshotThumbnailAsync(
                                userIds,
                                _avatarSettings.RenderDimensions,
                                Format.Png,
                                false
                            ).SyncOrDefault().Data?.FirstOrDefault()
                        );

                        return DownloadFile(url);
                    case ThumbnailCommandType.Avatar_R15_Action:
                        url = PollUntilCompleted(
                            userId,
                            () => _thumbnailsClient.GetAvatarThumbnailAsync(
                                userIds,
                                _avatarSettings.RenderDimensions,
                                Format.Png,
                                false
                            ).SyncOrDefault().Data?.FirstOrDefault()
                        );

                        return DownloadFile(url);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(thumbnailCommandType), thumbnailCommandType, null);
                }
            }
        );

        using var file = File.OpenRead(path);
        var memoryStream = new MemoryStream();

        file.CopyTo(memoryStream);

        return (memoryStream, Path.GetFileName(path));
    }

    /// <inheritdoc cref="IAvatarUtility.RenderUser(long,long,int,int)"/>
    public (Stream, string) RenderUser(long userId, long placeId, int sizeX, int sizeY)
    {
        if (_percentageInvoker.CanInvoke(_avatarSettings.RbxThumbnailsRolloutPercent))
        {
            _avatarThumbnailsOptedInToRbxThumbnailsTotal.WithLabels(userId.ToString()).Inc();

            _logger.Warning(
                "Trying to fetch the thumbnail for user '{0}' via rbx-thumbnails with the dimensions of {1}",
                userId,
                _avatarSettings.RenderDimensions
            );

            var commandType = _random.Next(0, 10) < 6
                ? ThumbnailCommandType.Avatar_R15_Action
                : ThumbnailCommandType.Closeup;

            return GetThumbnail(userId, commandType);
        }


        var url = GetAvatarFetchUrl(userId, placeId);

        _logger.Warning(
            "Trying to render user '{0}' in place '{1}' with the dimensions of {2}x{3} with the url '{4}'",
            userId,
            placeId,
            sizeX,
            sizeY,
            url
        );

        var thumbType = _random.Next(0, 10) < 6
            ? ThumbnailCommandType.Avatar_R15_Action
            : ThumbnailCommandType.Closeup;

        _avatarThumbnailsOptedOutOfRbxThumbnailsTotal.WithLabels(userId.ToString(), placeId.ToString(), thumbType.ToString(), sizeX.ToString(), sizeY.ToString()).Inc();

        var settings = new ThumbnailSettings(thumbType, GetThumbnailArgs(url, sizeX, sizeY).ToArray());

#if !PRE_JSON_EXECUTION
        var renderScript = new ThumbnailCommand(settings);
#else
        var renderScript = Lua.NewScript(
            thumbType.ToString(),
            ScriptProvider.GetScript(thumbType),
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

                var result = soap.BatchJobEx(
                    new GridJob()
                    {
                        id = Guid.NewGuid().ToString(),
                        expirationInSeconds = _avatarSettings.RenderJobTimeout.TotalSeconds
                    },
                    renderScript
                );

                Task.Run(() => _jobManager.CloseJob(job, true));

                var first = result.ElementAt(0);
                if (first != null)
                    return (new MemoryStream(Convert.FromBase64String(first.value)), GetFileName(userId, placeId, settings));

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

    private string GetAvatarFetchUrl(long userId, long placeId)
        => $"{_avatarSettings.AvatarFetchUrl}?userId={userId}&placeId={placeId}";

    private string GetFileName(long userId, long placeId, ThumbnailSettings settings)
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
}
