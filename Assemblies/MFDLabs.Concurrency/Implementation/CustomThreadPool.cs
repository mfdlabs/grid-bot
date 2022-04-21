using System;
using System.Threading;
using MFDLabs.Logging;
using Microsoft.Ccr.Core;

#if !NETFRAMEWORK
using MFDLabs.Instrumentation;
#else
using System.Diagnostics;
#endif

namespace MFDLabs.Concurrency
{
    /// <summary>
    /// An implementation for a custom thread pool that uses the Concurrent Runtime.
    /// </summary>
    public sealed class CustomThreadPool : IDisposable
    {
        private class WaitQueueItem
        {
            public WaitCallback Callback;
            public ExecutionContext Context;
            public object State;
        }

#if !NETFRAMEWORK
        private sealed class CustomThreadPoolPerformanceMonitor
        {
            public IRawValueCounter DispatcherQueueCount { get; }
            public IRateOfCountsPerSecondCounter DispatcherQueueCurrentSchedulingRate { get; }
            public IRawValueCounter DispatcherQueueScheduledTaskCount { get; }
            public IRawValueCounter DispatcherPendingTaskCount { get; }
            public IRawValueCounter DispatcherProcessedTaskCount { get; }
            public IRawValueCounter DispatcherWorkerThreadCount { get; }

            public CustomThreadPoolPerformanceMonitor(string perfCategory, ICounterRegistry cr)
            {
                if (perfCategory == null) throw new ArgumentNullException(nameof(perfCategory));
                if (cr == null) throw new ArgumentNullException(nameof(cr));

                DispatcherQueueCount = cr.GetRawValueCounter(perfCategory, "Dispatcher Queue Count");
                DispatcherQueueCurrentSchedulingRate = cr.GetRateOfCountsPerSecondCounter(perfCategory, "Dispatcher Queue Current Scheduling Rate");
                DispatcherQueueScheduledTaskCount = cr.GetRawValueCounter(perfCategory, "Dispatcher Queue Scheduled Task Count");
                DispatcherPendingTaskCount = cr.GetRawValueCounter(perfCategory, "Dispatcher Pending Task Count");
                DispatcherProcessedTaskCount = cr.GetRawValueCounter(perfCategory, "Dispatcher Processed Task Count");
                DispatcherWorkerThreadCount = cr.GetRawValueCounter(perfCategory, "Dispatcher Worker Thread Count");
            }
        }
#endif

#if !NETFRAMEWORK
        private readonly CustomThreadPoolPerformanceMonitor _perfmon;
#endif
        private Dispatcher _Dispatcher;
        private DispatcherQueue _DispatcherQueue;
        private static readonly string _PerformanceCategory = "MFDLabs.Concurrency.CustomThreadPool";
        private readonly Port<WaitQueueItem> _WaitQueueItemsPort = new();


#pragma warning disable CS1572 // XML comment has a param tag, but there is no parameter by that name
        /// <summary>
        /// Constructs a new instance of <see cref="CustomThreadPool"/>
        /// </summary>
        /// <param name="counterRegistry">A perf counter registry.</param>
        /// <param name="name">The name of this thread pool</param>
        /// <param name="threadCount">The total worker thread count for the internal dispatcher.</param>
        public CustomThreadPool(
#pragma warning restore CS1572 // XML comment has a param tag, but there is no parameter by that name
#if !NETFRAMEWORK
            ICounterRegistry counterRegistry,
#endif
            string name,
            int threadCount
        )
        {
#if !NETFRAMEWORK
            if (counterRegistry == null) throw new ArgumentNullException(nameof(counterRegistry));
            _perfmon = new(_PerformanceCategory, counterRegistry);
#endif

            _Dispatcher = new Dispatcher(threadCount, ThreadPriority.Normal, DispatcherOptions.UseBackgroundThreads, string.Format("{0} Dispatcher", name));
            _DispatcherQueue = new PatchedDispatcherQueue(string.Format("{0} Dispatcher Queue", name), _Dispatcher);
            Arbiter.Activate(_DispatcherQueue, Arbiter.Receive(true, _WaitQueueItemsPort, ExecuteWorkItem));
            new Thread(() => MonitorPerformance(name)) { IsBackground = true, Name = string.Format("Performance Monitor: {0}", name) }.Start();
        }

        /// <summary>
        /// The amount of items in the DispatcherQueue
        /// </summary>
        public int QueueCount { get { CheckDisposed(); return _DispatcherQueue.Count; } }

        /// <summary>
        /// The total items in the dispatcher that are waiting to be processed.
        /// </summary>
        public int PendingTaskCount { get { CheckDisposed(); return _DispatcherQueue.Dispatcher.PendingTaskCount; } }

        /// <summary>
        /// The total items that have been processed.
        /// </summary>
        public long ProcessedTaskCount { get { CheckDisposed(); return _DispatcherQueue.Dispatcher.ProcessedTaskCount; } }

        /// <summary>
        /// The total number of WorkerThreads inside the dispatcher.
        /// </summary>
        public int WorkerThreadCount { get { CheckDisposed(); return _DispatcherQueue.Dispatcher.WorkerThreadCount; } }

        private void CheckDisposed() { if (_DispatcherQueue == null) throw new ObjectDisposedException(GetType().Name); }
        private void ExecuteWorkItem(WaitQueueItem item)
        {
            try { ExecutionContext.Run(item.Context, item.Callback.Invoke, item.State); }
            catch (ThreadAbortException) { }
            catch (ThreadInterruptedException) { }
            catch (Exception ex) { SystemLogger.Singleton.Error(ex); }
        }
        private void MonitorPerformance(string instanceName)
        {
            try
            {
#if NETFRAMEWORK
                if (!PerformanceCounterCategory.Exists(_PerformanceCategory))
                {
                    var collection = new CounterCreationDataCollection
                    {
                        new CounterCreationData("Dispatcher Queue Count", string.Empty, PerformanceCounterType.NumberOfItems32),
                        new CounterCreationData("Dispatcher Queue Current Scheduling Rate", string.Empty, PerformanceCounterType.RateOfCountsPerSecond64),
                        new CounterCreationData("Dispatcher Queue Scheduled Task Count", string.Empty, PerformanceCounterType.NumberOfItems64),
                        new CounterCreationData("Dispatcher Pending Task Count", string.Empty, PerformanceCounterType.NumberOfItems32),
                        new CounterCreationData("Dispatcher Processed Task Count", string.Empty, PerformanceCounterType.NumberOfItems64),
                        new CounterCreationData("Dispatcher Worker Thread Count", string.Empty, PerformanceCounterType.NumberOfItems32)
                    };
                    PerformanceCounterCategory.Create(_PerformanceCategory, string.Empty, PerformanceCounterCategoryType.SingleInstance, collection);
                }

                var perfDispatcherQueueCount = new PerformanceCounter(_PerformanceCategory, "Dispatcher Queue Count", false);
                var perfDispatcherQueueCurrentSchedulingRate = new PerformanceCounter(_PerformanceCategory, "Dispatcher Queue Current Scheduling Rate", false);
                var perfDispatcherQueueScheduledTaskCount = new PerformanceCounter(_PerformanceCategory, "Dispatcher Queue Scheduled Task Count", false);
                var perfDispatcherPendingTaskCount = new PerformanceCounter(_PerformanceCategory, "Dispatcher Pending Task Count", false);
                var perfDispatcherProcessedTaskCount = new PerformanceCounter(_PerformanceCategory, "Dispatcher Processed Task Count", false);
                var perfDispatcherWorkerThreadCount = new PerformanceCounter(_PerformanceCategory, "Dispatcher Worker Thread Count", false);
                var num = _DispatcherQueue.ScheduledTaskCount;

                while (true)
                {
                    perfDispatcherQueueCount.RawValue = _DispatcherQueue.Count;
                    var scheduledTaskCount = _DispatcherQueue.ScheduledTaskCount;
                    perfDispatcherQueueCurrentSchedulingRate.IncrementBy(scheduledTaskCount - num);
                    num = scheduledTaskCount;
                    perfDispatcherQueueScheduledTaskCount.RawValue = num;
                    perfDispatcherPendingTaskCount.RawValue = _DispatcherQueue.Dispatcher.PendingTaskCount;
                    perfDispatcherProcessedTaskCount.RawValue = _DispatcherQueue.Dispatcher.ProcessedTaskCount;
                    perfDispatcherWorkerThreadCount.RawValue = _DispatcherQueue.Dispatcher.WorkerThreadCount;
                    Thread.Sleep(500);
                }
#else
                var num = _DispatcherQueue.ScheduledTaskCount;

                while (true)
                {
                    _perfmon.DispatcherQueueCount.Set(_DispatcherQueue.Count);
                    var scheduledTaskCount = _DispatcherQueue.ScheduledTaskCount;
                    _perfmon.DispatcherQueueCurrentSchedulingRate.IncrementBy(scheduledTaskCount - num);
                    num = scheduledTaskCount;
                    _perfmon.DispatcherQueueScheduledTaskCount.Set(num);
                    _perfmon.DispatcherPendingTaskCount.Set(_DispatcherQueue.Dispatcher.PendingTaskCount);
                    _perfmon.DispatcherProcessedTaskCount.Set(_DispatcherQueue.Dispatcher.ProcessedTaskCount);
                    _perfmon.DispatcherWorkerThreadCount.Set(_DispatcherQueue.Dispatcher.WorkerThreadCount);
                    Thread.Sleep(500);
                }
#endif
            }
            catch (ThreadAbortException) { }
            catch (Exception ex) { SystemLogger.Singleton.Error(ex); }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _DispatcherQueue?.Dispose();
            _DispatcherQueue = null;
            _Dispatcher?.Dispose();
            _Dispatcher = null;
        }

        /// <summary>
        /// Queue a user work item without a state
        /// </summary>
        /// <param name="callback">The callback to invoke in the new thread.</param>
        public void QueueUserWorkItem(WaitCallback callback)
            => QueueUserWorkItem(callback, null);

        /// <summary>
        /// Queue a user work item with a state
        /// </summary>
        /// <param name="callback">The callback to invoke in the new thread.</param>
        /// <param name="state">The state to pass into the thread.</param>
        /// <exception cref="ArgumentNullException">The callback is null.</exception>
        public void QueueUserWorkItem(WaitCallback callback, object state)
        {
            CheckDisposed();
            var item = new WaitQueueItem
            {
                Callback = callback ?? throw new ArgumentNullException(nameof(callback)),
                State = state,
                Context = ExecutionContext.Capture()
            };
            _WaitQueueItemsPort.Post(item);
        }
    }
}
