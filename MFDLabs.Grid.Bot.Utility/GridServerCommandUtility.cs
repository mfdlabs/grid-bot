using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MFDLabs.Diagnostics;
using MFDLabs.Grid.Commands;
using MFDLabs.Grid.ComputeCloud;
using MFDLabs.Logging;
using MFDLabs.Networking;

#if NETFRAMEWORK
using Microsoft.Win32;
#endif

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBeMadeStatic.Global

namespace MFDLabs.Grid.Bot.Utility
{
    public static class GridServerCommandUtility
    {
        public enum ScriptType
        {
            InternalScript,
            InternalModule,
            ThumbnailScript,
            ThumbnailModule,
            LuaPackage,
            SharedCoreScript,
            ClientCoreScript,
            CoreModule,
            ServerCoreScript,
            StarterCharacterScript,
            StarterPlayerScript,
            StarterPlayerScriptNewStructure,
            StarterPlayerScriptCommon,
            HiddenCommon,
            Hidden,
            HiddenModule
        }

        private static object GetGridServerPath(bool throwIfNoGridServer = true)
        {
#if NETFRAMEWORK
            var value = Registry.GetValue(
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerRegistryKeyName,
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerRegistryValueName,
                null);
#else
            var value = Environment.GetEnvironmentVariable("GRID_SERVER_LOCATION");
#endif
            if (value != null) return value;
            if (throwIfNoGridServer) 
                throw new ApplicationException($"The grid server was not correctly installed on the machine '{SystemGlobal.GetMachineId()}', " +
                                               $"please contact the datacenter administrator to sort this out.");
            return default;
        }

        public static string GetGridServerScriptPath(string scriptName,
            ScriptType scriptType = ScriptType.InternalScript,
            bool throwIfNoGridServer = true,
            bool test = false)
        {
            var prefix = GetGridServerPrefixByScriptType(scriptType, throwIfNoGridServer);

            scriptName = scriptName.Replace("..", "");
            var fullPath = $"{prefix}{scriptName}.lua";

            if (!test) return fullPath;
            
            if (!File.Exists(fullPath)) 
                throw new ApplicationException($"Unable to find the script file '{scriptName}' at the path '{prefix}'");

            return fullPath;
        }

        private static string GetGridServerPrefixByScriptType(ScriptType scriptType, bool throwIfNoGridServer = true)
        {
            var prefix = (string)GetGridServerPath(throwIfNoGridServer);
            prefix += scriptType switch
            {
                ScriptType.InternalScript => "internalscripts\\scripts\\",
                ScriptType.InternalModule => "internalscripts\\modules\\",
                ScriptType.ThumbnailScript => "internalscripts\\thumbnails\\",
                ScriptType.ThumbnailModule => "internalscripts\\thumbnails\\modules\\",
                ScriptType.LuaPackage => "ExtraContent\\LuaPackages\\",
                ScriptType.SharedCoreScript => "ExtraContent\\scripts\\CoreScripts\\",
                ScriptType.ClientCoreScript => "ExtraContent\\scripts\\CoreScripts\\CoreScripts\\",
                ScriptType.CoreModule => "ExtraContent\\scripts\\CoreScripts\\Modules\\",
                ScriptType.ServerCoreScript => "ExtraContent\\scripts\\CoreScripts\\ServerCoreScripts\\",
                ScriptType.StarterCharacterScript => "ExtraContent\\scripts\\PlayerScripts\\StarterCharacterScripts\\",
                ScriptType.StarterPlayerScript => "ExtraContent\\scripts\\PlayerScripts\\StarterPlayerScripts\\",
                ScriptType.StarterPlayerScriptNewStructure =>
                    "ExtraContent\\scripts\\PlayerScripts\\StarterPlayerScripts_NewStructure\\",
                ScriptType.StarterPlayerScriptCommon =>
                    "ExtraContent\\scripts\\PlayerScripts\\StarterPlayerScriptsCommon\\",
                ScriptType.HiddenCommon => "ExtraContent\\hidden\\common\\",
                ScriptType.Hidden => "Content\\hidden\\rcc\\",
                ScriptType.HiddenModule => "Content\\hidden\\rcc\\modules\\",
                _ => throw new ArgumentOutOfRangeException(nameof(scriptType), scriptType, null)
            };

            return prefix;
        }

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

            SystemLogger.Singleton.Warning(
                "Trying to render user '{0}' in place '{1}' with the dimensions of {2}x{3} with the url '{4}'",
                userId,
                placeId,
                sizeX,
                sizeY,
                url);

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
                    "Render Queue",
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

                SystemLogger.Singleton.Warning("Trying to convert the reponse from a Base64 string via: Convert.FromBase64String()");

                var first = result.ElementAt(0);
                if (first != null)
                {
                    //setting when for how much is truncated ;-;
                    SystemLogger.Singleton.Warning("Returning image contents of '{0}...[Truncated]'.",
                        first.value.Substring(0,
                            25));

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

        private static string GetAvatarFetchUrl(long userId, long placeId)
        {
            if (userId == -200000) 
                throw new Exception("Test exception for handlers to hit.");
            return $"https://{(global::MFDLabs.Grid.Bot.Properties.Settings.Default.RemoteRenderTaskAvatarFetchHost)}" +
                   $"{(global::MFDLabs.Grid.Bot.Properties.Settings.Default.RemoteRenderAvatarFetchUriPart)}?userId={userId}&placeId={placeId}";
        }

        public static LuaValue[] LaunchSimpleGame(string jobId, long placeId, long universeId)
        {
            return GridServerArbiter.Singleton.OpenJobEx(
                "Game Server Queue",
                new Job() { id = jobId, expirationInSeconds = 20000 },
                new ScriptExecution()
                {
                    name = "Execute Script",
                    script = JsonScriptingUtility.GetSharedGameServerScript(jobId, placeId, universeId).Item1
                }
            );
        }

        public static Task<LuaValue[]> LaunchSimpleGameAsync(string jobId, long placeId, long universeId)
        {
            return GridServerArbiter.Singleton.OpenJobExAsync(
                "Game Server Queue",
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
