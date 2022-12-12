using System;
using MFDLabs.Analytics.Google;
using MFDLabs.Grid.Bot.Global;
using MFDLabs.Grid.Bot.PerformanceMonitors;
using MFDLabs.Grid.Bot.Properties;
using MFDLabs.Logging;
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
				ShutdownUdpReceiver.Stop();
                if (killBot)
                    await BotGlobal.TryLogout();
                GridProcessHelper.KillAllGridServersSafe();
                GridProcessHelper.KillServerSafe();
                Logger.TryClearLocalLog(false);
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
				ShutdownUdpReceiver.Stop();
                if (!global::MFDLabs.Grid.Properties.Settings.Default.SingleInstancedGridServer)
                    GridServerArbiter.Singleton.KillAllOpenInstances();
                if (killBot)
                    await BotGlobal.TryLogout();
                Logger.TryClearLocalLog(true);
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
                Logger.TryClearLocalLog(true);
                await BotGlobal.SingletonLaunch();

                if (!restartServers) return;

                if (global::MFDLabs.Grid.Properties.Settings.Default.SingleInstancedGridServer)
                {
                    GridProcessHelper.KillAllGridServersSafe();
                    GridProcessHelper.KillServerSafe();
                    GridProcessHelper.OpenServerSafe();
                }
                else
                {
                    GridProcessHelper.KillAllGridServersSafe();
                }

                PerformanceServer.Stop();
                PerformanceServer.Start();
            }, TimeSpan.FromSeconds(1));
        }
    }
}