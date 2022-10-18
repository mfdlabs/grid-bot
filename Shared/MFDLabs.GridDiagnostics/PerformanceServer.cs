using System.Linq;
using System.Net;
using System.Collections.Generic;
using MFDLabs.Logging;
using MFDLabs.Instrumentation;
using MFDLabs.Text.Extensions;

#if !DEBUG
using MFDLabs.Diagnostics;
#endif

namespace MFDLabs.Grid.Bot.PerformanceMonitors
{
    public static class PerformanceServer
    {
        private static IEnumerable<string> WhitelistedCounterServerCidrs =>
            (from id in global::MFDLabs.Grid.Bot.Properties.Settings.Default.WhitelistedCounterServerCidrs.Split(',')
             where !id.IsNullOrEmpty()
             select id).ToArray();

        private static readonly CounterHttpServer Server = new(
            PerfmonCounterRegistryProvider.Registry,
            global::MFDLabs.Grid.Bot.Properties.Settings.Default.CounterServerPort,
            WhitelistedCounterServerCidrs,
            ex =>
            {
                if (ex is not HttpListenerException {ErrorCode: 0x3E3})
                {
#if DEBUG || DEBUG_LOGGING_IN_PROD
                    Logger.Singleton.Error(ex);
#else
                    Logger.Singleton.Warning("An error occurred on the perfmon counter server, please review this error message: {0}.", ex.Message);
#endif
                }
            }
        );

        public static void Start()
        {
            Logger.Singleton.LifecycleEvent("Launching performance monitor server...");

#if !DEBUG

            if (!SystemGlobal.ContextIsAdministrator())
            {
                Logger.Singleton.Warning("Not launching performance monitor service due to context accessibility issues.");
                return;
            }

#endif

            Server.Start();
            Logger.Singleton.Warning("Launched performance monitor server on host 'http://*:{0}'.",
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.CounterServerPort);
        }

        public static void Stop()
        {
            Logger.Singleton.LifecycleEvent("Stopping performance monitor server on host 'http://*:{0}'...", global::MFDLabs.Grid.Bot.Properties.Settings.Default.CounterServerPort);

#if !DEBUG

            if (!SystemGlobal.ContextIsAdministrator())
            {
                Logger.Singleton.Warning("Not stopping performance monitor service due to context accessibility issues.");
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

            Logger.Singleton.Warning("Stopped performance monitor server on port {0}.", global::MFDLabs.Grid.Bot.Properties.Settings.Default.CounterServerPort);
        }
    }
}
