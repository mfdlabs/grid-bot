using System;
using MFDLabs.Abstractions;
using MFDLabs.Analytics.Google;
using MFDLabs.Grid.Bot.Global;
using MFDLabs.Grid.Bot.PerformanceMonitors;
using MFDLabs.Logging;
using MFDLabs.Logging.Diagnostics;
using MFDLabs.Networking;
using MFDLabs.Threading;

namespace MFDLabs.Grid.Bot.Utility
{
    public sealed class SignalUtility : SingletonBase<SignalUtility>
    {
        public void InvokeInteruptSignal()
        {
            Console.WriteLine("Got SIGINT. Will start shutdown procedure within 1 second.");

            TaskHelper.SetTimeout(async () =>
            {
                GoogleAnalyticsManager.Singleton.TrackNetworkEvent("Shutdown", "SIGINT", "Shutdown via SIGINT", 1);
                PerformanceServer.Singleton.Stop();
                await BotGlobal.Singleton.TryLogout();
                SystemUtility.Singleton.KillAllDeployersSafe();
                SystemUtility.Singleton.KillAllGridServersSafe();
                SystemUtility.Singleton.KillServerSafe();
                LoggingSystem.Singleton.EndLifetimeWatch();
                SystemLogger.Singleton.TryClearLocalLog(false, true);
                Environment.Exit(0);
            }, TimeSpan.FromSeconds(1));
        }

        public void InvokeUserSignal1()
        {
            Console.WriteLine("Got SIGUSR1. Will exit app without closing child processes (only if single instanced) with 1 second.");

            TaskHelper.SetTimeout(async () =>
            {
                GoogleAnalyticsManager.Singleton.TrackNetworkEvent("Shutdown", "SIGUSR1", "Shutdown via SIGINT", 1);
                PerformanceServer.Singleton.Stop();
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) GridServerArbiter.Singleton.KillAllOpenInstances();
                SystemUtility.Singleton.KillAllDeployersSafe();
                await BotGlobal.Singleton.TryLogout();
                LoggingSystem.Singleton.EndLifetimeWatch();
                SystemLogger.Singleton.TryClearLocalLog(false, true);
                Environment.Exit(0);
            }, TimeSpan.FromSeconds(1));
        }

        public void InvokeUserSignal2(bool restartServers = false)
        {
            Console.WriteLine("Got SIGUSR2. Will close all child processes, and clear LocalLog within 1 second.");

            TaskHelper.SetTimeout(async () =>
            {
                GoogleAnalyticsManager.Singleton.TrackNetworkEvent("Restart", "SIGUSR2", "Restart via SIGINT", 1);
                await BotGlobal.Singleton.TryLogout();
                SystemLogger.Singleton.TryClearLocalLog(true, false);
                LoggingSystem.Singleton.RestartLifetimeWatch();
                await BotGlobal.Singleton.SingletonLaunch();
                SystemUtility.Singleton.KillAllDeployersSafe();
                if (restartServers)
                {
                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer)
                    {
                        SystemUtility.Singleton.KillAllGridServersSafe();
                        SystemUtility.Singleton.KillServerSafe();
                        SystemUtility.Singleton.OpenGridServerSafe();
                    } 
                    else
                    {
                        SystemUtility.Singleton.KillAllGridServersSafe();
                    }
                    PerformanceServer.Singleton.Stop();
                    PerformanceServer.Singleton.Start();
                }
            }, TimeSpan.FromSeconds(1));
        }
    }
}
