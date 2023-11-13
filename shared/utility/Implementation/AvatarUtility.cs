namespace Grid.Bot.Utility;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Logging;

using Random;

using Grid.Commands;

using GridJob = Grid.ComputeCloud.Job;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBeMadeStatic.Global

/// <summary>
/// Utility to be used when interacting with the rendering
/// layer of the grid servers.
/// </summary>
/// <remarks>
/// Construct a new instance of <see cref="AvatarUtility"/>.
/// </remarks>
/// <param name="logger">The <see cref="ILogger"/>.</param>
/// <param name="avatarSettings">The <see cref="AvatarSettings"/>.</param>
/// <param name="random">The <see cref="IRandom"/>.</param>
/// <param name="jobManager">The <see cref="IJobManager"/>.</param>
/// <exception cref="ArgumentNullException">
/// - <paramref name="logger"/> cannot be null.
/// - <paramref name="avatarSettings"/> cannot be null.
/// - <paramref name="random"/> cannot be null.
/// - <paramref name="jobManager"/> cannot be null.
/// </exception>
public class AvatarUtility(
    ILogger logger,
    AvatarSettings avatarSettings,
    IRandom random,
    IJobManager jobManager
) : IAvatarUtility
{
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly AvatarSettings _avatarSettings = avatarSettings ?? throw new ArgumentNullException(nameof(avatarSettings));
    private readonly IRandom _random = random ?? throw new ArgumentNullException(nameof(random));
    private readonly IJobManager _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));

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

    /// <inheritdoc cref="IAvatarUtility.RenderUser(long,long,int,int)"/>
    public (Stream, string) RenderUser(long userId, long placeId, int sizeX, int sizeY)
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
