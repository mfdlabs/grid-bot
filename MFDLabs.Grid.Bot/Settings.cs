using Discord;
using MFDLabs.Abstractions;
using System;

namespace MFDLabs.Grid.Bot
{
    internal sealed class Settings : SingletonBase<Settings>
    {
        internal string HigherPrivilagedUsers
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.HigherPrivilagedUsers;
            }
        }
        internal bool StopProcessingOnNullPacketItem
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.StopProcessingOnNullPacketItem;
            }
        }
        internal bool DebugAllowTaskCanceledExceptions
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.DebugAllowTaskCanceledExceptions;
            }
        }
        internal TimeSpan ScreenshotRelayExpiration
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScreenshotRelayExpiration;
            }
        }
        internal TimeSpan RenderQueueExpiration
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderQueueExpiration;
            }
        }
        internal bool OpenGridServerAtStartup
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenGridServerAtStartup;
            }
        }
        internal bool HideProcessWindows
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.HideProcessWindows;
            }
        }
        internal bool UserUtilityShouldResolveBannedUsers
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.UserUtilityShouldResolveBannedUsers;
            }
        }
        internal bool RenderThumbnailTypeShouldForceCloseup
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderThumbnailTypeShouldForceCloseup;
            }
        }
        internal int CounterServerPort
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.CounterServerPort;
            }
        }
        internal bool ShouldLaunchCounterServer
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.ShouldLaunchCounterServer;
            }
        }
        internal bool AllowAdminsToBypassDisabledCommands
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.AllowAdminsToBypassDisabledCommands;
            }
        }
        internal bool RenderJobShouldDeleteResult
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderJobShouldDeleteResult;
            }
        }
        internal int RenderJobTimeoutInSeconds
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderJobTimeoutInSeconds;
            }
        }
        internal bool AllowLogSettingsInPublicChannels
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.AllowLogSettingsInPublicChannels;
            }
        }
        internal bool AllowNullsWhenUpdatingSetting
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.AllowNullsWhenUpdatingSetting;
            }
        }
        internal bool NewThreadsOnlyAvailableForAdmins
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.NewThreadsOnlyAvailableForAdmins;
            }
        }
        internal string BaseURL
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.BaseURL;
            }
        }
        internal bool AdminScriptPrependBaseURL
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.AdminScriptPrependBaseURL;
            }
        }
        internal bool ExecuteCommandsInNewThread
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.ExecuteCommandsInNewThread;
            }
        }
        internal bool OnLaunchWarnAboutAdminMode
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.OnLaunchWarnAboutAdminMode;
            }
        }
        internal bool AdminScriptsOnlyAllowedByOwner
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.AdminScriptsOnlyAllowedByOwner;
            }
        }
        internal bool AllowAdminScripts
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.AllowAdminScripts;
            }
        }
        internal string GridServerRegistryValueName
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerRegistryValueName;
            }
        }
        internal string GridServerRegistryKeyName
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerRegistryKeyName;
            }
        }
        internal bool ViewConsoleEnabled
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.ViewConsoleEnabled;
            }
        }
        internal bool ScriptExectionEnabled
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScriptExectionEnabled;
            }
        }
#if DEBUG
        internal bool OnLaunchWarnAboutDebugMode
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.OnLaunchWarnAboutDebugMode;
            }
        }
#endif
        internal bool ScriptExectionCareAboutBadTextCase
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScriptExectionCareAboutBadTextCase;
            }
        }
        internal bool ScriptExectionSupportUnicode
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScriptExectionSupportUnicode;
            }
        }
        internal string BlacklistedScriptKeywords
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.BlacklistedScriptKeywords;
            }
        }
        internal bool KillCommandShouldForce
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.KillCommandShouldForce;
            }
        }
        internal bool CareToLeakSensitiveExceptions
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.CareToLeakSensitiveExceptions;
            }
        }
        internal int RenderSizeY
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderSizeY;
            }
        }
        internal int RenderSizeX
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderSizeX;
            }
        }
        internal long RenderPlaceID
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderPlaceID;
            }
        }
        internal string RenderResultFileName
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderResultFileName;
            }
        }
        internal string RemoteRenderAvatarFetchUriPart
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.RemoteRenderAvatarFetchUriPart;
            }
        }
        internal string RemoteRenderAssetFetchUrl
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.RemoteRenderAssetFetchUrl;
            }
        }
        internal string RemoteRenderTaskAvatarFetchHost
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.RemoteRenderTaskAvatarFetchHost;
            }
        }
        internal long MaxUserIDSize
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.MaxUserIDSize;
            }
        }
        internal bool RenderingEnabled
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderingEnabled;
            }
        }
        internal TimeSpan UsersServiceCircuitBreakerRetryInterval
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.UsersServiceCircuitBreakerRetryInterval;
            }
        }
        internal int UsersServiceMaxCircuitBreakerFailuresBeforeTrip
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.UsersServiceMaxCircuitBreakerFailuresBeforeTrip;
            }
        }
        internal TimeSpan UsersServiceRequestTimeout
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.UsersServiceRequestTimeout;
            }
        }
        internal int UsersServiceMaxRedirects
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.UsersServiceMaxRedirects;
            }
        }
        internal string UsersServiceRemoteURL
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.UsersServiceRemoteURL;
            }
        }
        internal TimeSpan RenderQueueDelay
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderQueueDelay;
            }
        }
        internal ActivityType BotGlobalActivityType
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotGlobalActivityType;
            }
        }
        internal string BotGlobalStreamURL
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotGlobalStreamURL;
            }
        }
        internal string BotToken
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotToken;
            }
        }
        internal ulong BotOwnerID
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotOwnerID;
            }
        }
        internal bool RegisterCommandRegistryAtAppStart
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.RegisterCommandRegistryAtAppStart;
            }
        }
        internal TimeSpan ScreenshotRelayActivationTimeout
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScreenshotRelayActivationTimeout;
            }
        }
        internal string ScreenshotRelayOutputFilename
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScreenshotRelayOutputFilename;
            }
        }
        internal string ScreenshotRelayExecutableName
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScreenshotRelayExecutableName;
            }
        }
        internal bool ScreenshotRelayShouldShowLauncherWindow
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScreenshotRelayShouldShowLauncherWindow;
            }
        }
        internal string AllowedChannels
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.AllowedChannels;
            }
        }
        internal string Admins
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.Admins;
            }
        }
        internal string LoggingUtilDataName
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.LoggingUtilDataName;
            }
        }
        internal bool PersistLocalLogs
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.PersistLocalLogs;
            }
        }
        internal TimeSpan SoapUtilityRemoteServiceTimeout
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.SoapUtilityRemoteServiceTimeout;
            }
        }
        internal int SoapUtilityRemoteServicePort
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.SoapUtilityRemoteServicePort;
            }
        }
        internal bool OpenServiceOnEndpointNotFoundException
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException;
            }
        }
        internal string GridServerDeployerExecutableName
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerDeployerExecutableName;
            }
        }
        internal bool GridServerDeployerShouldShowLauncherWindow
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerDeployerShouldShowLauncherWindow;
            }
        }

        internal bool AllowAllChannels
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.AllowAllChannels;
            }
        }
        internal bool IsEnabled
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.IsEnabled;
            }
        }
        internal string ReasonForDying
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.ReasonForDying;
            }
        }
        internal string Prefix
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.Prefix;
            }
        }
        internal UserStatus BotGlobalUserStatus
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotGlobalUserStatus;
            }
        }
        internal string BotGlobalStatusMessage
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotGlobalStatusMessage;
            }
        }
        internal bool IsAllowedToEchoBackNotFoundCommandException
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.IsAllowedToEchoBackNotFoundCommandException;
            }
        }
        internal bool ShouldLogDiscordInternals
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.ShouldLogDiscordInternals;
            }
        }
        internal bool AllowParsingForBots
        {
            get
            {
                return global::MFDLabs.Grid.Bot.Properties.Settings.Default.AllowParsingForBots;
            }
        }
    }
}
