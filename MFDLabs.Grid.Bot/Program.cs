using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Analytics.Google;
using MFDLabs.Diagnostics;
using MFDLabs.ErrorHandling.Extensions;
using MFDLabs.Grid.Bot.Events;
using MFDLabs.Grid.Bot.Global;
using MFDLabs.Grid.Bot.PerformanceMonitors;
using MFDLabs.Grid.Bot.Registries;
using MFDLabs.Grid.Bot.Utility;
using MFDLabs.Logging;
using MFDLabs.Networking;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Grid.Bot
{
    public sealed class Program
    {
#if DEBUG
        private const string _DebugMode = "WARNING: RUNNING IN DEBUG MODE, THIS CAN POTENTIALLY LEAK MORE INFORMATION THAN NEEDED, PLEASE RUN THIS ON RELEASE FOR PRODUCTION SCENARIOS.";
#endif
        private const string _AdminMode = "WARNING: RUNNING AS ADMINSTRATOR, THIS CAN POTENTIALLY BE DANGEROUS SECURITY WISE, PLEASE KNOW WHAT YOU ARE DOING!";
        private const string _PrimaryTaskError = "An exception occurred when trying to execute the primary task, please review the error message below: ";
        private const string _InitializationError = "An exception occurred when trying to initialize the bot network, please review the error message below: ";
        private const string _NoBotToken = "The setting \"BotToken\" was null when it is required.";

        public static void Main()
        {
            if (!File.Exists(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile))
            {
                if (File.Exists($"{Directory.GetCurrentDirectory()}\\app.config"))
                {
                    AppDomain.CurrentDomain.SetupInformation.ConfigurationFile = $"{Directory.GetCurrentDirectory()}\\app.config";
                }
                else
                {
                    throw new FileNotFoundException($"Could not locate the application configuration at the files '{AppDomain.CurrentDomain.SetupInformation.ConfigurationFile}' or '{Directory.GetCurrentDirectory()}\\app.config' with your distribution, please install the app correctly and try again.", $"'{AppDomain.CurrentDomain.SetupInformation.ConfigurationFile}' or '{Directory.GetCurrentDirectory()}\\app.config'");
                }
            }

            GoogleAnalyticsManager.Singleton.Initialize(global::MFDLabs.Grid.Bot.Properties.Settings.Default.GoogleAnalyticsTrackerID);

#if DEBUG
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OnLaunchWarnAboutDebugMode)
            {
                GoogleAnalyticsManager.Singleton.TrackEvent(NetworkingGlobal.Singleton.LocalIP, "Startup", "Warning", "Debug Mode Enabled", 1);
                SystemLogger.Singleton.Warning(_DebugMode);
            }
#endif
            if (SystemGlobal.Singleton.ContextIsAdministrator() && global::MFDLabs.Grid.Bot.Properties.Settings.Default.OnLaunchWarnAboutAdminMode)
            {
                GoogleAnalyticsManager.Singleton.TrackNetworkEvent("Startup", "Warning", "Administrator Context", 1);
                SystemLogger.Singleton.Warning(_AdminMode);
            }

            GoogleAnalyticsManager.Singleton.TrackNetworkEvent(
                "Startup",
                "Info",
                string.Format(
                    "Process '{0}' opened with file name '{1}' (version {2}).",
                    SystemGlobal.Singleton.CurrentProcess.Id.ToString("x"),
                    SystemGlobal.Singleton.CurrentProcess.ProcessName,
                    SystemGlobal.Singleton.AssemblyVersion
                ),
                1
            );

            SystemLogger.Singleton.Debug(
                "Process '{0}' opened with file name '{1}' (version {2}).",
                SystemGlobal.Singleton.CurrentProcess.Id.ToString("x"),
                SystemGlobal.Singleton.CurrentProcess.ProcessName,
                SystemGlobal.Singleton.AssemblyVersion
            );

            Console.Title = $"[{SystemGlobal.Singleton.CurrentProcess.Id:X}] '{SystemGlobal.Singleton.CurrentProcess.ProcessName}' @ '{SystemGlobal.Singleton.AssemblyVersion}' [{NetworkingGlobal.Singleton.GetLocalIP()}@{SystemGlobal.Singleton.GetMachineHost()} ({SystemGlobal.Singleton.GetMachineID()})]";

            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.ShouldLaunchCounterServer)
            {
                GoogleAnalyticsManager.Singleton.TrackNetworkEvent("Startup", "Info", "Performance Server Started", 1);
                PerformanceServer.Singleton.Start();
            }

            try
            {
                MainAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                GoogleAnalyticsManager.Singleton.TrackNetworkEvent("Startup", "Error", $"Startup Failure: {ex.ToDetailedString()}.", 1);
                SystemLogger.Singleton.LifecycleEvent(_PrimaryTaskError);
                SystemLogger.Singleton.Error(ex.Message);
                PerformanceServer.Singleton.Stop();
                Console.ReadKey(true);
            }
        }

        private static async Task MainAsync()
        {
            try
            {
                ConsoleHookRegistry.Singleton.Register();

                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotToken.IsNullOrWhiteSpace())
                {
                    GoogleAnalyticsManager.Singleton.TrackEvent(NetworkingGlobal.Singleton.LocalIP, "MainTask", "Error", $"MainTask Failure: No Bot Token.", 1);
                    SystemLogger.Singleton.Error(_NoBotToken);
                    SignalUtility.Singleton.InvokeInteruptSignal();
                    await Task.Delay(-1);
                }
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.RegisterCommandRegistryAtAppStart)
                    CommandRegistry.Singleton.RegisterOnce();

                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenGridServerAtStartup && global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer)
                    SystemUtility.Singleton.OpenGridServerSafe();

                BotGlobal.Singleton.Initialize(new DiscordSocketClient());

                BotGlobal.Singleton.Client.Log += OnLogMessage.Invoke;
                BotGlobal.Singleton.Client.LoggedIn += OnLoggedIn.Invoke;
                BotGlobal.Singleton.Client.LoggedOut += OnLoggedOut.Invoke;
                BotGlobal.Singleton.Client.Ready += OnReady.Invoke;
                BotGlobal.Singleton.Client.Connected += OnConnected.Invoke;
                BotGlobal.Singleton.Client.MessageReceived += OnMessage.Invoke;
                BotGlobal.Singleton.Client.Disconnected += OnDisconnected.Invoke;
                BotGlobal.Singleton.Client.LatencyUpdated += OnLatencyUpdated.Invoke;
                BotGlobal.Singleton.Client.JoinedGuild += OnBotGlobalAddedToGuild.Invoke;

                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OnStartCloseAllOpenGridServerInstances)
                    SystemUtility.Singleton.KillAllGridServersSafe();

                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OnStartBatchAllocate25ArbiterInstances)
                    ThreadPool.QueueUserWorkItem((s) =>
                    {
                        GridServerArbiter.Singleton.BatchQueueUpArbiteredInstancesUnsafe(5, 5);
                    });

                await BotGlobal.Singleton.SingletonLaunch();
                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                GoogleAnalyticsManager.Singleton.TrackNetworkEvent("MainTask", "Error", $"MainTask Failure: {ex.ToDetailedString()}.");
                SystemLogger.Singleton.LifecycleEvent(_InitializationError);
                SystemLogger.Singleton.Error(ex.Message);
                SignalUtility.Singleton.InvokeInteruptSignal();
                await Task.Delay(-1);
            }
        }
    }
}
