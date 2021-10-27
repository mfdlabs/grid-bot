﻿using System;
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
                Manager.Singleton.TrackNetworkEvent("Shutdown", "SIGINT", "Shutdown via SIGINT", 1);
                PerformanceServer.Singleton.Stop();
                await BotGlobal.Singleton.TryLogout();
                SystemUtility.Singleton.KillGridServerSafe();
                SystemUtility.Singleton.KillServerSafe();
                LoggingSystem.Singleton.EndLifetimeWatch();
                SystemLogger.Singleton.TryClearLocalLog(false, true);
                Environment.Exit(0);
            }, TimeSpan.FromSeconds(1));
        }

        public void InvokeUserSignal1()
        {
            Console.WriteLine("Got SIGUSR1. Will exit app without closing child processes with 1 second.");

            TaskHelper.SetTimeout(async () =>
            {
                Manager.Singleton.TrackNetworkEvent("Shutdown", "SIGUSR1", "Shutdown via SIGINT", 1);
                PerformanceServer.Singleton.Stop();
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
                Manager.Singleton.TrackNetworkEvent("Restart", "SIGUSR2", "Restart via SIGINT", 1);
                await BotGlobal.Singleton.TryLogout();
                SystemLogger.Singleton.TryClearLocalLog(true, false);
                LoggingSystem.Singleton.RestartLifetimeWatch();
                await BotGlobal.Singleton.SingletonLaunch();
                if (restartServers)
                {
                    SystemUtility.Singleton.KillGridServerSafe();
                    SystemUtility.Singleton.KillServerSafe();
                    PerformanceServer.Singleton.Stop();
                    PerformanceServer.Singleton.Start();
                    SystemUtility.Singleton.OpenGridServer();
                }
            }, TimeSpan.FromSeconds(1));
        }
    }
}
