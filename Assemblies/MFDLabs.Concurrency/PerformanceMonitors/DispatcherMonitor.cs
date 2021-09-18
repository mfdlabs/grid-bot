using Microsoft.Ccr.Core;
using System;
using System.Threading;

namespace MFDLabs.Concurrency
{
    internal class DispatcherMonitor : IDisposable
    {
        private readonly Thread monitorThread;
        internal DispatcherMonitor(Dispatcher _)
        {
            var thread = new Thread(() => { })
            {
                IsBackground = true,
                Name = "Perfmon ConcurrencyService"
            };
            thread.Start();
            monitorThread = thread;
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (monitorThread != null)
                monitorThread.Abort();
        }

        #endregion
    }
}