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
    public sealed class SignalUtility
    {
        public static void InvokeInteruptSignal()
        {
            Console.WriteLine(Resources.SignalUtility_InvokeInteruptSignal);

            TaskHelper.SetTimeout(async () =>
            {
                await GoogleAnalyticsManager.Singleton.TrackNetworkEventAsync("Shutdown", "SIGINT", "Shutdown via SIGINT", 1);
                PerformanceServer.Singleton.Stop();
                await BotGlobal.TryLogout();
                SystemUtility.KillAllDeployersSafe();
                SystemUtility.KillAllGridServersSafe();
                SystemUtility.KillServerSafe();
                LoggingSystem.EndLifetimeWatch();
                SystemLogger.Singleton.TryClearLocalLog(false, true);
                Environment.Exit(0);
            }, TimeSpan.FromSeconds(1));
        }

        public static void InvokeUserSignal1()
        {
            Console.WriteLine(Resources.SignalUtility_InvokeUserSignal1);

            TaskHelper.SetTimeout(async () =>
            {
                await GoogleAnalyticsManager.Singleton.TrackNetworkEventAsync("Shutdown", "SIGUSR1", "Shutdown via SIGINT", 1);
                PerformanceServer.Singleton.Stop();
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) GridServerArbiter.Singleton.KillAllOpenInstances();
                SystemUtility.KillAllDeployersSafe();
                await BotGlobal.TryLogout();
                LoggingSystem.EndLifetimeWatch();
                SystemLogger.Singleton.TryClearLocalLog(false, true);
                Environment.Exit(0);
            }, TimeSpan.FromSeconds(1));
        }

        public static void InvokeUserSignal2(bool restartServers = false)
        {
            Console.WriteLine(Resources.SignalUtility_InvokeUserSignal2);

            TaskHelper.SetTimeout(async () =>
            {
                await GoogleAnalyticsManager.Singleton.TrackNetworkEventAsync("Restart", "SIGUSR2", "Restart via SIGINT", 1);
                await BotGlobal.TryLogout();
                SystemLogger.Singleton.TryClearLocalLog(true, false);
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
                PerformanceServer.Singleton.Stop();
                PerformanceServer.Singleton.Start();
            }, TimeSpan.FromSeconds(1));
        }
    }
}
