namespace Grid.Bot.Utility;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Logging;

using Random;
using Text.Extensions;

using Grid.Commands;
using Grid.ComputeCloud;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBeMadeStatic.Global

/// <summary>
/// Utility to be used when interacting with the rendering
/// layer of the grid servers.
/// </summary>
public static class AvatarUtility
{
    private static IEnumerable<object> GetThumbnailArgs(string url, int x, int y)
    {
        yield return AvatarSettings.Singleton.RenderAssetFetchUrl; // baseUrl
        yield return url; // characterAppearanceUrl
        yield return AvatarSettings.Singleton.RenderThumbnailType; // fileExtension 
        yield return x; // x
        yield return y; // y

        // these are specific to closeups.
        yield return true; // quadratic
        yield return 30; // baseHatZoom
        yield return 100; // maxHatZoom
        yield return 0; // cameraOffsetX
        yield return 0; // cameraOffsetY
    }

    /// <summary>
    /// Render a user by ID.
    /// </summary>
    /// <param name="userId">The user's Id.</param>
    /// <param name="placeId">The place Id to inherit character settings from.</param>
    /// <param name="sizeX">The X dimension of the image.</param>
    /// <param name="sizeY">The Y dimension of the image.</param>
    /// <returns>The stream and thumbnail name.</returns>
    public static (Stream, string) RenderUser(long userId, long placeId, int sizeX, int sizeY)
    {
        var url = GetAvatarFetchUrl(userId, placeId);

        Logger.Singleton.Warning(
            "Trying to render user '{0}' in place '{1}' with the dimensions of {2}x{3} with the url '{4}'",
            userId,
            placeId,
            sizeX,
            sizeY,
            url
        );

        var thumbType = RandomFactory.GetDefaultRandom().Next(0, 10) < 6 
            ? ThumbnailCommandType.Avatar_R15_Action 
            : ThumbnailCommandType.Closeup;

        var settings = new ThumbnailSettings(thumbType, GetThumbnailArgs(url, sizeX, sizeY).ToArray());
        var renderScript = new ThumbnailCommand(settings);

        try
        {
            var result = GridServerArbiter.Singleton.BatchJobEx(
                new Job()
                {
                    id = Guid.NewGuid().ToString(),
                    expirationInSeconds = AvatarSettings.Singleton.RenderJobTimeout.TotalSeconds
                },
                Lua.NewScript(
                    Guid.NewGuid().ToString(),
                    renderScript.ToJson()
                )
            );

            var first = result.ElementAt(0);
            if (first != null)
                return (new MemoryStream(Convert.FromBase64String(first.value)), GetFileName(userId, placeId, settings));

            Logger.Singleton.Error("The first return argument for the render was null, this may be an issue with the grid server.");

            return (null, null);
        }
        catch (Exception ex)
        {
            Logger.Singleton.Error(ex);

            return (null, null);
        }
    }

    private static string GetAvatarFetchUrl(long userId, long placeId) 
        => $"{AvatarSettings.Singleton.AvatarFetchUrl}?userId={userId}&placeId={placeId}";

    private static string GetFileName(long userId, long placeId, ThumbnailSettings settings)
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
               $"{args[9]}.{AvatarSettings.Singleton.RenderThumbnailType.ToLower()}";
    }
}
