﻿using MFDLabs.Abstractions;
using MFDLabs.Diagnostics;
using MFDLabs.Instrumentation;
using MFDLabs.Logging;
using System.Net;

namespace MFDLabs.Grid.Bot.PerformanceMonitors
{
    internal sealed class PerformanceServer : SingletonBase<PerformanceServer>
    {
        private readonly CounterHttpServer _server = new CounterHttpServer(
            StaticCounterRegistry.Instance,
            Settings.Singleton.CounterServerPort,
            (ex) =>
            {
                if (!(ex is HttpListenerException httpEx && httpEx.ErrorCode == 0x3E3))
                {
#if DEBUG
                    SystemLogger.Singleton.Error(ex);
#else
                    SystemLogger.Singleton.Warning("An error occurred on the perfmon counter server, please review this error message: {0}.", ex.Message);
#endif
                }
            }
        );

        internal void Start()
        {
            SystemLogger.Singleton.LifecycleEvent("Launching performance monitor server...");

            if (!SystemGlobal.Singleton.ContextIsAdministrator())
            {
                SystemLogger.Singleton.Warning("Not launching performance monitor service due to context accessibility issues.");
                return;
            }

            _server.Start();
            SystemLogger.Singleton.Warning("Launched performance monitor server on host 'http://*:{0}'.", Settings.Singleton.CounterServerPort);
        }

        internal void Stop()
        {
            SystemLogger.Singleton.LifecycleEvent("Stopping performance monitor server on host 'http://*:{0}'...", Settings.Singleton.CounterServerPort);

            if (!SystemGlobal.Singleton.ContextIsAdministrator())
            {
                SystemLogger.Singleton.Warning("Not stopping performance monitor service due to context accessibility issues.");
                return;
            }

            try
            {
                _server.Stop();
            }
            catch
            {
            }

            SystemLogger.Singleton.Warning("Stopped performance monitor server on port {0}.", Settings.Singleton.CounterServerPort);
        }
    }
}
