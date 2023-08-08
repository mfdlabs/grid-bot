using System;

using Logging;

using MFDLabs.Threading;
using MFDLabs.Grid.Bot.Global;
using MFDLabs.Grid.Bot.Properties;
using MFDLabs.Grid.Bot.PerformanceMonitors;

namespace MFDLabs.Grid.Bot.Utility
{
    public static class SignalUtility
    {
        public static void InvokeInteruptSignal(bool killBot = true)
        {
            Console.WriteLine(Resources.SignalUtility_InvokeInteruptSignal);

            TaskHelper.SetTimeout(async () =>
            {
                PerformanceServer.Stop();
                ShutdownUdpReceiver.Stop();
                if (killBot)
                    await BotRegistry.TryLogout();
                try { GridServerArbiter.Singleton.KillAllInstances(); } catch { }
                Logger.TryClearLocalLog(false);
                Environment.Exit(0);
            }, TimeSpan.FromSeconds(1));
        }

        public static void InvokeUserSignal1(bool killBot = true)
        {
            Console.WriteLine(Resources.SignalUtility_InvokeUserSignal1);

            TaskHelper.SetTimeout(async () =>
            {
                PerformanceServer.Stop();
                ShutdownUdpReceiver.Stop();
                try { GridServerArbiter.Singleton.KillAllInstances(); } catch { }
                if (killBot)
                    await BotRegistry.TryLogout();
                Logger.TryClearLocalLog(true);
                Environment.Exit(0);
            }, TimeSpan.FromSeconds(1));
        }

        public static void InvokeUserSignal2(bool restartServers = false, bool killBot = true)
        {
            Console.WriteLine(Resources.SignalUtility_InvokeUserSignal2);

            TaskHelper.SetTimeout(async () =>
            {
                if (killBot)
                    await BotRegistry.TryLogout();
                Logger.TryClearLocalLog(true);
                await BotRegistry.SingletonLaunch();

                if (!restartServers) return;

                try { GridServerArbiter.Singleton.KillAllInstances(); } catch { }

                PerformanceServer.Stop();
                PerformanceServer.Start();
            }, TimeSpan.FromSeconds(1));
        }
    }
}
