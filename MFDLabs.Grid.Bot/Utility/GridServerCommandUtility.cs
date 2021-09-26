using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MFDLabs.Abstractions;
using MFDLabs.Grid.Commands;
using MFDLabs.Grid.ComputeCloud;
using MFDLabs.Logging;
using MFDLabs.Networking;
using Microsoft.Win32;

namespace MFDLabs.Grid.Bot.Utility
{
    public sealed class GridServerCommandUtility : SingletonBase<GridServerCommandUtility>
    {
        public object GetGridServerPath() 
            => Registry.GetValue(Settings.Singleton.GridServerRegistryKeyName, Settings.Singleton.GridServerRegistryValueName, null);

        private IEnumerable<object> GetThumbnailArgs(string url, int x, int y)
        {
            yield return Settings.Singleton.RemoteRenderAssetFetchUrl; // baseUrl
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
            yield break;
        }

        //TODO: return tuple of filename, contents b64 encoded and contents b64 decoded?
        public (Stream, string) RenderUser(long userId, long placeId, int sizeX, int sizeY)
        {
            var url = GetAvatarFetchURL(userId, placeId);

            SystemLogger.Singleton.Warning("Trying to render user '{0}' in place '{1}' with the dimensions of {2}x{3} with the url '{4}'", userId, placeId, sizeX, sizeY, url);

            ThumbnailCommandType thumbType;

            if (Settings.Singleton.RenderThumbnailTypeShouldForceCloseup)
            {
                thumbType = ThumbnailCommandType.Closeup;
            }
            else
            {
                //yes well I essentially just moved the random inline because it is used once
                thumbType = new Random().Next(0, 10) < 6 ? ThumbnailCommandType.Avatar_R15_Action : ThumbnailCommandType.Closeup;
            }

            var (renderScript, settings) = JsonScriptingUtility.Singleton.GetThumbnailScript(thumbType, GetThumbnailArgs(url, sizeX, sizeY).ToArray());

            try
            {
                var result = SoapUtility.Singleton.BatchJobEx(
                    new Job()
                    {
                        id = NetworkingGlobal.Singleton.GenerateUUIDV4(),
                        expirationInSeconds = Settings.Singleton.RenderJobTimeoutInSeconds
                    },
                    Lua.NewScript(
                        NetworkingGlobal.Singleton.GenerateUUIDV4(),
                        renderScript
                    )
                );

                SystemLogger.Singleton.Warning("Trying to convert the reponse from a Base64 string via: Convert.FromBase64String()");

                var first = result.ElementAt(0);
                if (first != null)
                {
                    //setting when for how much is truncated ;-;
                    SystemLogger.Singleton.Warning("Returning image contents of '{0}...[Truncated]'.", first.value.Substring(0, 25));

                    return (new MemoryStream(Convert.FromBase64String(first.value)), GetFileName(userId, placeId, settings));
                }

                SystemLogger.Singleton.Error("The first return argument for the render was null, this may be an issue with the grid server.");

                return (null, null);
            }
            catch (Exception ex)
            {
                SystemLogger.Singleton.Error(ex);
                return (null, null);
            }
        }

        private static string GetAvatarFetchURL(long userId, long placeId)
        {
            if (userId == -200000) throw new Exception("Test exception for handlers to hit.");
            return string.Format(
                "https://{0}{1}?userId={2}&placeId={3}",
                Settings.Singleton.RemoteRenderTaskAvatarFetchHost,
                Settings.Singleton.RemoteRenderAvatarFetchUriPart,
                userId,
                placeId
            );
        }

        public LuaValue[] LaunchSimpleGame(string jobID, long placeID, long universeID)
        {
            return SoapUtility.Singleton.OpenJobEx(
                new Job() { id = jobID, expirationInSeconds = 20000 },
                new ScriptExecution()
                {
                    name = "Execute Script",
                    script = JsonScriptingUtility.Singleton.GetSharedGameServerScript(jobID, placeID, universeID).Item1
                }
            );
        }

        public Task<LuaValue[]> LaunchSimpleGameAsync(string jobID, long placeID, long universeID)
        {
            return SoapUtility.Singleton.OpenJobExAsync(
                new Job() { id = jobID, expirationInSeconds = 20000 },
                new ScriptExecution()
                {
                    name = "Execute Script",
                    script = JsonScriptingUtility.Singleton.GetSharedGameServerScript(jobID, placeID, universeID).Item1
                }
            );
        }

        private string GetFileName(long userID, long placeID, ThumbnailSettings settings)
        {
            var args = settings.Arguments;
            return $"{NetworkingGlobal.Singleton.GenerateUUIDV4()}_{userID}_{placeID}_{settings.Type}_{args[2]}_{args[3]}_{args[4]}_{args[5]}_{args[6]}_{args[7]}_{args[8]}_{args[9]}_{Settings.Singleton.RenderResultFileName}";
        }
    }
}
