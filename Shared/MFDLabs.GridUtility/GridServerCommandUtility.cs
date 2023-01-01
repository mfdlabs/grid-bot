using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MFDLabs.Grid.Commands;
using MFDLabs.Grid.ComputeCloud;
using MFDLabs.Logging;
using MFDLabs.Networking;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBeMadeStatic.Global

namespace MFDLabs.Grid.Bot.Utility
{
    public static class GridServerCommandUtility
    {
        private static IEnumerable<object> GetThumbnailArgs(string url, int x, int y)
        {
            yield return global::MFDLabs.Grid.Bot.Properties.Settings.Default.RemoteRenderAssetFetchUrl; // baseUrl
            yield return url; // characterAppearanceUrl
            yield return "PNG"; // fileExtension . TODO setting so I can do JPEGs also.
            yield return x; // x
            yield return y; // y
            // these are specific to closeups.
            yield return true; // quadratic
            yield return 30; // baseHatZoom
            yield return 100; // maxHatZoom
            yield return 0; // cameraOffsetX
            yield return 0; // cameraOffsetY
        }

        //TODO: return tuple of filename, contents b64 encoded and contents b64 decoded?
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

            ThumbnailCommandType thumbType;

            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderThumbnailTypeShouldForceCloseup)
            {
                thumbType = ThumbnailCommandType.Closeup;
            }
            else
            {
                //yes well I essentially just moved the random inline because it is used once
                thumbType = new Random().Next(0, 10) < 6 ? ThumbnailCommandType.Avatar_R15_Action : ThumbnailCommandType.Closeup;
            }

            var (renderScript, settings) = JsonScriptingUtility.GetThumbnailScript(thumbType, GetThumbnailArgs(url, sizeX, sizeY).ToArray());

            try
            {
                var result = GridServerArbiter.Singleton.BatchJobEx(
                    new Job()
                    {
                        id = NetworkingGlobal.GenerateUuidv4(),
                        expirationInSeconds = global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderJobTimeoutInSeconds
                    },
                    Lua.NewScript(
                        NetworkingGlobal.GenerateUuidv4(),
                        renderScript
                    )
                );

                Logger.Singleton.Warning("Trying to convert the reponse from a Base64 string via: Convert.FromBase64String()");

                var first = result.ElementAt(0);
                if (first != null)
                {
                    //setting when for how much is truncated ;-;
                    Logger.Singleton.Warning("Returning image contents of '{0}...[Truncated]'.", first.value.Substring(0, 150));

                    return (new MemoryStream(Convert.FromBase64String(first.value)), GetFileName(userId, placeId, settings));
                }

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
        {
            if (userId == -200000) 
                throw new Exception("Test exception for handlers to hit.");
            return $"https://{(global::MFDLabs.Grid.Bot.Properties.Settings.Default.RemoteRenderTaskAvatarFetchHost)}" +
                   $"{(global::MFDLabs.Grid.Bot.Properties.Settings.Default.RemoteRenderAvatarFetchUriPart)}?userId={userId}&placeId={placeId}";
        }

        public static LuaValue[] LaunchSimpleGame(string jobId, long placeId, long universeId)
        {
            return GridServerArbiter.Singleton
                .GetOrCreatePersistentInstance("Game Server Queue")
                .OpenJobEx(
                    new Job() { id = jobId, expirationInSeconds = 20000 },
                    new ScriptExecution()
                    {
                        name = "Execute Script",
                        script = JsonScriptingUtility.GetSharedGameServerScript(jobId, placeId, universeId).Item1
                    }
                );
        }

        public static async Task<LuaValue[]> LaunchSimpleGameAsync(string jobId, long placeId, long universeId)
        {
            return await GridServerArbiter.Singleton
                .GetOrCreatePersistentInstance("Game Server Queue")
                .OpenJobExAsync(
                    new Job() { id = jobId, expirationInSeconds = 20000 },
                    new ScriptExecution()
                    {
                        name = "Execute Script",
                        script = JsonScriptingUtility.GetSharedGameServerScript(jobId, placeId, universeId).Item1
                    }
                );
        }

        private static string GetFileName(long userId, long placeId, ThumbnailSettings settings)
        {
            var args = settings.Arguments;
            return $"{NetworkingGlobal.GenerateUuidv4()}_" +
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
                   $"{args[9]}_" +
                   $"{MFDLabs.Grid.Bot.Properties.Settings.Default.RenderResultFileName}";
        }
    }
}
