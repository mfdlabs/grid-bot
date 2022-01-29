using System.Net;
using MFDLabs.Diagnostics;
using MFDLabs.Instrumentation;
using MFDLabs.Logging;

namespace MFDLabs.Grid.Bot.PerformanceMonitors
{
    public static class PerformanceServer
    {
        private static readonly CounterHttpServer Server = new(
            PerfmonCounterRegistryProvider.Registry,
            global::MFDLabs.Grid.Bot.Properties.Settings.Default.CounterServerPort,
            ex =>
            {
                if (ex is not HttpListenerException {ErrorCode: 0x3E3})
                {
#if DEBUG || DEBUG_LOGGING_IN_PROD
                    SystemLogger.Singleton.Error(ex);
#else
                    SystemLogger.Singleton.Warning("An error occurred on the perfmon counter server, please review this error message: {0}.", ex.Message);
#endif
                }
            }
        );

        public static void Start()
        {
            SystemLogger.Singleton.LifecycleEvent("Launching performance monitor server...");

#if !DEBUG

            if (!SystemGlobal.ContextIsAdministrator())
            {
                SystemLogger.Singleton.Warning("Not launching performance monitor service due to context accessibility issues.");
                return;
            }

#endif

            Server.Start();
            SystemLogger.Singleton.Warning("Launched performance monitor server on host 'http://*:{0}'.",
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.CounterServerPort);
        }

        public static void Stop()
        {
            SystemLogger.Singleton.LifecycleEvent("Stopping performance monitor server on host 'http://*:{0}'...", global::MFDLabs.Grid.Bot.Properties.Settings.Default.CounterServerPort);

#if !DEBUG

            if (!SystemGlobal.ContextIsAdministrator())
            {
                SystemLogger.Singleton.Warning("Not stopping performance monitor service due to context accessibility issues.");
                return;
            }

#endif

            try
            {
                Server.Stop();
            }
            catch
            {
                // ignored
            }

            SystemLogger.Singleton.Warning("Stopped performance monitor server on port {0}.", global::MFDLabs.Grid.Bot.Properties.Settings.Default.CounterServerPort);
        }
    }
}
