using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Ccr.Core;

// ReSharper disable once CheckNamespace
namespace MFDLabs.Concurrency
{
    /// <summary>
    /// Used for debugging Dispatcher problems
    /// </summary>
    public class DispatcherMonitor : IDisposable
    {
        private readonly Dispatcher _dispatcher;
        private readonly ICollection<Thread> _threads = new HashSet<Thread>();

        /// <summary>
        /// Construct new dispatcher monitor
        /// </summary>
        /// <param name="dispatcher"></param>
        public DispatcherMonitor(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            GatherWorkerThreads();
            dispatcher.UnhandledException += dispatcher_UnhandledException;

            var thread = new Thread(Monitor)
            {
                IsBackground = true,
                Name = "DispatcherMonitor: " + dispatcher.Name
            };
            thread.Start();
        }

        private void dispatcher_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ExceptionHandler.LogException(new ApplicationException($"{_dispatcher.Name} had an unhandled exception",
                e.ExceptionObject as Exception));
        }

        private static void Monitor()
        {
            const int iterations = 4;
            var sleep = TimeSpan.Zero;
            while (true)
            {
                try
                {
                    sleep = TimeSpan.FromSeconds(10);
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    ExceptionHandler.LogException(ex);
                }

                if (sleep != TimeSpan.Zero)
                {
                    System.Threading.Thread.Sleep(sleep);
                    sleep = TimeSpan.Zero;
                }
                else
                {
                    var wait = TimeSpan.Parse("00:00:03");
                    if (wait != TimeSpan.Zero)
                        System.Threading.Thread.Sleep((int) (wait.TotalMilliseconds / iterations));
                    else
                        System.Threading.Thread.Sleep(TimeSpan.FromSeconds(10));
                }
            }
        }

        private void GatherWorkerThreads()
        {
            // Grab all the threads for debugging!
            // TODO: Make this more robust. It's pretty kludgy
            var count = 2 * _dispatcher.WorkerThreadCount;
            for (var i = 0; i < 2 * _dispatcher.WorkerThreadCount; ++i)
                _dispatcher.DispatcherQueues[0].Enqueue
                (Arbiter.FromHandler(() =>
                {
                    System.Threading.Thread.Sleep(200);
                    lock (_threads)
                        _threads.Add(System.Threading.Thread.CurrentThread);
                    System.Threading.Interlocked.Decrement(ref count);
                }));

            while (count > 0)
                System.Threading.Thread.Sleep(10);
        }

        /// <summary>
        /// For debugging only!
        /// </summary>
        /// <returns></returns>
        private IEnumerable<StackTrace> GetWorkerStacks()
        {
            lock (_threads)
            {
                foreach (var t in _threads)
                {
                    StackTrace trace;
#pragma warning disable 0618
                    t.Suspend();
                    try
                    {
                        trace = new System.Diagnostics.StackTrace(t, true);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                    finally
                    {
                        t.Resume();
                    }
#pragma warning restore 0618
                    yield return trace;
                }
            }
        }

        private void ReportBacklog(int pendingTasks)
        {
            var message = $"CcrService detected a backlog of {pendingTasks}. These are the currently running tasks:\r\n\r\n";
            message = GetWorkerStacks().Aggregate(message, (current, trace) => current + trace + "\r\n\r\n");
            ExceptionHandler.LogException(message, EventLogEntryType.Warning, 4061);
        }


        #region IDisposable Members

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_dispatcher != null)
                _dispatcher.Dispose();

            _threads.Clear();
        }

        #endregion
    }
}