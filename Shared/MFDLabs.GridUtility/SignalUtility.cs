using System;
using MFDLabs.Analytics.Google;
using MFDLabs.Grid.Bot.Global;
using MFDLabs.Grid.Bot.PerformanceMonitors;
using MFDLabs.Grid.Bot.Properties;
using MFDLabs.Logging;
using MFDLabs.Logging.Diagnostics;
using MFDLabs.Threading;

// ReSharper disable AsyncVoidLambda

namespace MFDLabs.Grid.Bot.Utility
{
    public static class SignalUtility
    {
        public static void InvokeInteruptSignal(bool killBot = true)
        {
            Console.WriteLine(Resources.SignalUtility_InvokeInteruptSignal);

            TaskHelper.SetTimeout(async () =>
            {
                await GoogleAnalyticsManager.TrackNetworkEventAsync("Shutdown", "SIGINT", "Shutdown via SIGINT");
                ConsulServiceRegistrationUtility.DeregisterService("MFDLabs.Grid.Bot");
                ConsulServiceRegistrationUtility.DeregisterService("MFDLabs.Grid.Bot.PerfmonServerV2");
                PerformanceServer.Stop();
                if (killBot)
                    await BotGlobal.TryLogout();
                SystemUtility.KillAllDeployersSafe();
                SystemUtility.KillAllGridServersSafe();
                SystemUtility.KillServerSafe();
                LoggingSystem.EndLifetimeWatch();
                SystemLogger.Singleton.TryClearLocalLog(false, true);
                Environment.Exit(0);
            }, TimeSpan.FromSeconds(1));
        }

        public static void InvokeUserSignal1(bool killBot = true)
        {
            Console.WriteLine(Resources.SignalUtility_InvokeUserSignal1);

            TaskHelper.SetTimeout(async () =>
            {
                await GoogleAnalyticsManager.TrackNetworkEventAsync("Shutdown", "SIGUSR1", "Shutdown via SIGINT");
                ConsulServiceRegistrationUtility.DeregisterService("MFDLabs.Grid.Bot");
                ConsulServiceRegistrationUtility.DeregisterService("MFDLabs.Grid.Bot.PerfmonServerV2");
                PerformanceServer.Stop();
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer)
                    GridServerArbiter.Singleton.KillAllOpenInstances();
                if (killBot)
                    SystemUtility.KillAllDeployersSafe();
                await BotGlobal.TryLogout();
                LoggingSystem.EndLifetimeWatch();
                SystemLogger.Singleton.TryClearLocalLog(false, true);
                Environment.Exit(0);
            }, TimeSpan.FromSeconds(1));
        }

        public static void InvokeUserSignal2(bool restartServers = false, bool killBot = true)
        {
            Console.WriteLine(Resources.SignalUtility_InvokeUserSignal2);

            TaskHelper.SetTimeout(async () =>
            {
                await GoogleAnalyticsManager.TrackNetworkEventAsync("Restart", "SIGUSR2",
                    "Restart via SIGINT");

                if (killBot)
                    await BotGlobal.TryLogout();
                SystemLogger.Singleton.TryClearLocalLog(true);
                LoggingSystem.RestartLifetimeWatch();
                await BotGlobal.SingletonLaunch();
                SystemUtility.KillAllDeployersSafe();

                if (!restartServers) return;

                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer)
                {
                    SystemUtility.KillAllGridServersSafe();
                    SystemUtility.KillServerSafe();
                    SystemUtility.OpenGridServerSafe();
                }
                else
                {
                    SystemUtility.KillAllGridServersSafe();
                }

                PerformanceServer.Stop();
                PerformanceServer.Start();
            }, TimeSpan.FromSeconds(1));
        }
    }
}