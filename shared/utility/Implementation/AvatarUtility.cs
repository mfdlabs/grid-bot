namespace Grid.Bot.Utility;

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

using Random;
using Logging;
using FileSystem;
using Thumbnails.Client;
using Threading.Extensions;

using Grid.Commands;

using GridJob = Grid.ComputeCloud.Job;

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

    private readonly ExpirableDictionary<(long, ThumbnailCommandType), string> _localCachedPaths;

    /// <summary>
    /// Construct a new instance of <see cref="AvatarUtility"/>.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="avatarSettings">The <see cref="AvatarSettings"/>.</param>
    /// <param name="random">The <see cref="IRandom"/>.</param>
    /// <param name="jobManager">The <see cref="IJobManager"/>.</param>
    /// <param name="thumbnailsClient">The <see cref="IThumbnailsClient"/>.</param>
    /// <param name="percentageInvoker">The <see cref="IPercentageInvoker"/>.</param>
    public AvatarUtility(
        ILogger logger,
        AvatarSettings avatarSettings,
        IRandom random,
        IJobManager jobManager,
        IThumbnailsClient thumbnailsClient,
        IPercentageInvoker percentageInvoker
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _avatarSettings = avatarSettings ?? throw new ArgumentNullException(nameof(avatarSettings));
        _random = random ?? throw new ArgumentNullException(nameof(random));
        _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
        _thumbnailsClient = thumbnailsClient ?? throw new ArgumentNullException(nameof(thumbnailsClient));
        _percentageInvoker = percentageInvoker ?? throw new ArgumentNullException(nameof(percentageInvoker));

        _localCachedPaths = new(avatarSettings.LocalCacheTtl);
        _localCachedPaths.EntryRemoved += OnLocalCacheEntryRemoved;
    }

    private void OnLocalCacheEntryRemoved(string path, RemovalReason reason)
    {
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

    private static string PollUntilCompleted(Func<ThumbnailResponse> func)
    {
        var response = func() ?? throw new ThumbnailResponseException(ThumbnailResponseState.Error, "The thumbnail response was null.");

        if (response.State == ThumbnailResponseState.Completed)
            return response.ImageUrl;

        if (response.State != ThumbnailResponseState.Pending)
            throw new ThumbnailResponseException(response.State.GetValueOrDefault(), "The thumbnail response was not pending.");

        while (response.State == ThumbnailResponseState.Pending)
        {
            Task.Delay(1000).Wait();

            response = func();
        }

        if (response.State != ThumbnailResponseState.Completed)
            throw new ThumbnailResponseException(response.State.GetValueOrDefault(), "The thumbnail response was not completed.");

        return response.ImageUrl;
    }

    private static string DownloadFile(string url)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");
        var file = File.OpenWrite(path);

        using var client = new HttpClient();
        using var stream = client.GetStreamAsync(url).SyncOrDefault();

        stream.CopyTo(file);

        file.Close();
        file.Dispose();

        return path;
    }

    private (Stream, string) GetThumbnail(long userId, ThumbnailCommandType thumbnailCommandType)
    {
        if (thumbnailCommandType != ThumbnailCommandType.Closeup && thumbnailCommandType != ThumbnailCommandType.Avatar_R15_Action)
            throw new ArgumentException("The thumbnail command type must be either closeup or avatar_r15_action.", nameof(thumbnailCommandType));

        var userIds = new[] { userId };

        var path = _localCachedPaths.GetOrAdd(
            (userId, thumbnailCommandType),
            (key) =>
            {
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

        var settings = new ThumbnailSettings(thumbType, GetThumbnailArgs(url, sizeX, sizeY).ToArray());
        var renderScript = new ThumbnailCommand(settings);

        var job = new Job(Guid.NewGuid().ToString());

        try
        {
            var (soap, _, rejectionReason) = _jobManager.NewJob(job, _avatarSettings.RenderJobTimeout.TotalSeconds, true);

            if (rejectionReason != null)
            {
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

                _logger.Error("The first return argument for the render was null, this may be an issue with the grid server.");

                return (null, null);
            }
        }
        catch (Exception ex)
        {
            Task.Run(() => _jobManager.CloseJob(job, false));
            _logger.Error(ex);

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
