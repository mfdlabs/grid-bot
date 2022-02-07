using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Analytics.Google;
using MFDLabs.Diagnostics;
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
        private const string PrimaryTaskError = "An exception occurred when trying to execute the primary task, please check back trace!";
        private const string NoBotToken = "The setting \"BotToken\" was null when it is required.";
        private const string BadActorMessage = "THIS SOFTWARE IS UNLICENSED, IF YOU DO NOT HAVE EXPLICIT WRITTEN PERMISSION " +
                                               "BY THE CONTRIBUTORS OR THE PRIMARY DEVELOPER TO USE THIS, DELETE IT IMMEDIATELY!";

        public static void OnGlobalException(Exception ex)
        {
            GoogleAnalyticsManager.TrackNetworkEvent(
                "Startup",
                "Error",
                $"Startup Failure: {ex.Message}."
            );
            SystemLogger.Singleton.LifecycleEvent(PrimaryTaskError);
            PerformanceServer.Stop();
        }

        public static void Invoke(string[] args)
        {
            SystemLogger.Singleton.LifecycleEvent(BadActorMessage);

            GoogleAnalyticsManager.Initialize(
                PerfmonCounterRegistryProvider.Registry,
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.GoogleAnalyticsTrackerID
            );
#if DEBUG
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OnLaunchWarnAboutDebugMode)
            {
                GoogleAnalyticsManager.TrackNetworkEvent("Startup", "Warning", "Debug Mode Enabled");
                SystemLogger.Singleton.Warning(DebugMode);
            }
#endif
            if (SystemGlobal.ContextIsAdministrator() &&
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.OnLaunchWarnAboutAdminMode)
            {
                GoogleAnalyticsManager.TrackNetworkEvent("Startup", "Warning", "Administrator Context");
                SystemLogger.Singleton.Warning(AdminMode);
            }

            GoogleAnalyticsManager.TrackNetworkEvent(
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
                SystemGlobal.GetMachineId()
            );

            ConsulServiceRegistrationUtility.RegisterService("MFDLabs.Grid.Bot", true, null, null, new[] { "C#", ".NET" });
            ConsulServiceRegistrationUtility.RegisterSubService(
                "MFDLabs.Grid.Bot",
                "MFDLabs.Grid.Bot.PerfmonServerV2",
                false,
                NetworkingGlobal.GetLocalIp(),
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.CounterServerPort,
                new[] { "perf", "node-perf", "perf-counter-v2" }
            );
            ConsulServiceRegistrationUtility.RegisterServiceHttpCheck(
                "MFDLabs.Grid.Bot.PerfmonServerV2",
                "Counter Server Health Check",
                $"http://{NetworkingGlobal.GetLocalIp()}:{(global::MFDLabs.Grid.Bot.Properties.Settings.Default.CounterServerPort)}",
                "Health For Counter Server"
            );

            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.ShouldLaunchCounterServer)
            {
                GoogleAnalyticsManager.TrackNetworkEvent("Startup", "Info", "Performance Server Started");
                PerformanceServer.Start();
            }

            InvokeAsync(args).Wait();
        }

        private static async Task InvokeAsync(IEnumerable<string> args)
        {
            // For Unix, skip this, as I assume we won't need this:)
            ConsoleHookRegistry.Register();

            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotToken.IsNullOrWhiteSpace())
            {
                await GoogleAnalyticsManager.TrackNetworkEventAsync(
                    "MainTask",
                    "Error",
                    "MainTask Failure: No Bot Token."
                );
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
#if DEBUG || DEBUG_LOGGING_IN_PROD
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
                GridProcessHelper.KillAllGridServersSafe();

            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenGridServerAtStartup &&
                global::MFDLabs.Grid.Properties.Settings.Default.SingleInstancedGridServer)
                GridProcessHelper.OpenServerSafe();

            var defaultHttpBinding = new BasicHttpBinding(BasicHttpSecurityMode.None)
            {
                MaxReceivedMessageSize = int.MaxValue,
                SendTimeout = global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterDefaultTimeout
            };

            GridServerArbiter.SetDefaultHttpBinding(defaultHttpBinding);
            GridServerArbiter.SetCounterRegistry(PerfmonCounterRegistryProvider.Registry);

            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterQueueUpEnabled &&
                !global::MFDLabs.Grid.Properties.Settings.Default.SingleInstancedGridServer)
                GridServerArbiter.Singleton.SetupPool();

            SingleInstancedArbiter.SetBinding(defaultHttpBinding);

            if (!args.Contains("--no-gateway"))
                await BotGlobal.SingletonLaunch();
            await Task.Delay(-1);
        }
    }
}