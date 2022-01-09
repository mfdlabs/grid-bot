using System;
using System.IO;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Analytics.Google;
using MFDLabs.Diagnostics;
using MFDLabs.ErrorHandling.Extensions;
using MFDLabs.Grid.Bot.Events;
using MFDLabs.Grid.Bot.Global;
using MFDLabs.Grid.Bot.PerformanceMonitors;
using MFDLabs.Grid.Bot.Properties;
using MFDLabs.Grid.Bot.Registries;
using MFDLabs.Grid.Bot.Utility;
using MFDLabs.Logging;
using MFDLabs.Networking;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Grid.Bot
{
    internal static class Runner
    {
#if DEBUG
        private const string DebugMode = "WARNING: RUNNING IN DEBUG MODE, THIS CAN POTENTIALLY LEAK MORE INFORMATION " +
                                         "THAN NEEDED, PLEASE RUN THIS ON RELEASE FOR PRODUCTION SCENARIOS.";
#endif
        private const string AdminMode = "WARNING: RUNNING AS ADMINSTRATOR, THIS CAN POTENTIALLY BE DANGEROUS " +
                                         "SECURITY WISE, PLEASE KNOW WHAT YOU ARE DOING!";
        private const string PrimaryTaskError = "An exception occurred when trying to execute the primary task, please review the error message below: ";
        private const string InitializationError = "An exception occurred when trying to initialize the bot network, please review the error message below: ";
        private const string NoBotToken = "The setting \"BotToken\" was null when it is required.";
        private const string BadActorMessage = "THIS SOFTWARE IS UNLICENSED, IF YOU DO NOT HAVE EXPLICIT WRITTEN PERMISSION " +
                                               "BY THE CONTRIBUTORS OR THE PRIMARY DEVELOPER TO USE THIS, DELETE IT IMMEDIATELY";

        public static void Invoke()
        {
            SystemLogger.Singleton.LifecycleEvent(BadActorMessage);

            GoogleAnalyticsManager.Singleton.Initialize(global::MFDLabs.Grid.Bot.Properties.Settings.Default.GoogleAnalyticsTrackerID);
#if DEBUG
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OnLaunchWarnAboutDebugMode)
            {
                GoogleAnalyticsManager.Singleton.TrackNetworkEvent("Startup", "Warning", "Debug Mode Enabled");
                SystemLogger.Singleton.Warning(DebugMode);
            }
#endif
            if (SystemGlobal.ContextIsAdministrator() &&
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.OnLaunchWarnAboutAdminMode)
            {
                GoogleAnalyticsManager.Singleton.TrackNetworkEvent("Startup", "Warning", "Administrator Context");
                SystemLogger.Singleton.Warning(AdminMode);
            }
            GoogleAnalyticsManager.Singleton.TrackNetworkEvent(
                "Startup",
                "Info",
                $"Process '{SystemGlobal.CurrentProcess.Id:x}' " +
                $"opened with file name '{SystemGlobal.CurrentProcess.ProcessName}' at path " +
                $"'{Directory.GetCurrentDirectory()}' (version {SystemGlobal.AssemblyVersion})."
            );

            SystemLogger.Singleton.Debug(
                "Process '{0}' opened with file name '{1}' at path '{2}' (version {3}).",
                SystemGlobal.CurrentProcess.Id.ToString("x"),
                SystemGlobal.CurrentProcess.ProcessName,
                Directory.GetCurrentDirectory(),
                SystemGlobal.AssemblyVersion
            );

            Console.Title = string.Format(Resources.Runner_Invoke_Title,
                SystemGlobal.CurrentProcess.Id,
                SystemGlobal.CurrentProcess.ProcessName,
                SystemGlobal.AssemblyVersion,
                NetworkingGlobal.GetLocalIp(),
                SystemGlobal.GetMachineHost(),
                SystemGlobal.GetMachineId());

            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.ShouldLaunchCounterServer)
            {
                GoogleAnalyticsManager.Singleton.TrackNetworkEvent("Startup", "Info", "Performance Server Started");
                PerformanceServer.Start();
            }

            try
            {
                InvokeAsync().Wait();
            }
            catch (Exception ex)
            {
                GoogleAnalyticsManager.Singleton.TrackNetworkEvent("Startup", "Error", $"Startup Failure: {ex.ToDetailedString()}.");
                SystemLogger.Singleton.LifecycleEvent(PrimaryTaskError);
#if DEBUG
                SystemLogger.Singleton.Error(ex);
#else
                SystemLogger.Singleton.Error(ex.Message);
#endif
                PerformanceServer.Stop();
                Console.ReadKey(true);
            }
        }

        private static async Task InvokeAsync()
        {
            try
            {
                ConsoleHookRegistry.Register();

                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotToken.IsNullOrWhiteSpace())
                {
                    await GoogleAnalyticsManager.Singleton.TrackNetworkEventAsync("MainTask", "Error",
                        "MainTask Failure: No Bot Token.");
                    SystemLogger.Singleton.Error(NoBotToken);
                    // Case here so backtrace can catch potential hackers trying to use this without a token
                    // (they got assemblies but no configuration)
                    throw new InvalidOperationException(NoBotToken);
                }

                BotGlobal.Initialize(
                    new DiscordSocketClient(
                        new DiscordSocketConfig
                        {
                            GatewayIntents =
                                GatewayIntents.GuildMessages
                                | GatewayIntents.DirectMessages
                                | GatewayIntents.Guilds,
                            LogGatewayIntentWarnings = false,
#if DEBUG
                            LogLevel = LogSeverity.Debug,
#endif
                        }
                    )
                );

                BotGlobal.Client.Log += OnLogMessage.Invoke;
                BotGlobal.Client.LoggedIn += OnLoggedIn.Invoke;
                BotGlobal.Client.LoggedOut += OnLoggedOut.Invoke;
                BotGlobal.Client.Ready += OnReady.Invoke;
                BotGlobal.Client.Connected += OnConnected.Invoke;
                BotGlobal.Client.MessageReceived += OnMessage.Invoke;
                BotGlobal.Client.LatencyUpdated += OnLatencyUpdated.Invoke;
                BotGlobal.Client.JoinedGuild += OnBotGlobalAddedToGuild.Invoke;

#if WE_LOVE_EM_SLASH_COMMANDS
                BotGlobal.Client.SlashCommandExecuted += OnSlashCommand.Invoke;
#endif // WE_LOVE_EM_SLASH_COMMANDS

                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OnStartCloseAllOpenGridServerInstances)
                    SystemUtility.KillAllGridServersSafe();

                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenGridServerAtStartup &&
                    global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer)
                    SystemUtility.OpenGridServerSafe();

                GridServerArbiter.SetDefaultHttpBinding(
                    new BasicHttpBinding(BasicHttpSecurityMode.None)
                        {
                            MaxReceivedMessageSize = int.MaxValue,
                            SendTimeout = global::MFDLabs.Grid.Bot.Properties.Settings.Default.SoapUtilityRemoteServiceTimeout
                        }
                    );

                GridServerArbiter.Singleton.SetupPool();

                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OnStartBatchAllocate25ArbiterInstances &&
                    !global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer)
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        GridServerArbiter.Singleton.BatchQueueUpArbiteredInstancesUnsafe(25);
                    });

                await BotGlobal.SingletonLaunch();
                await Task.Delay(-1);
            }
            catch (InvalidOperationException)
            {
                // HACK: see the above message.
                // We may remove these try-catches entirely from this thread and just allow
                // the AppDomain's UnhandledException to hit
                throw;
            }
            catch (Exception ex)
            {
                await GoogleAnalyticsManager.Singleton.TrackNetworkEventAsync("MainTask", "Error",
                    $"MainTask Failure: {ex.ToDetailedString()}.");
                SystemLogger.Singleton.LifecycleEvent(InitializationError);
#if DEBUG
                SystemLogger.Singleton.Error(ex);
#else
                SystemLogger.Singleton.Error(ex.Message);
#endif
                SignalUtility.InvokeInteruptSignal();
                await Task.Delay(-1);
            }
        }
    }
}
