using MFDLabs.ErrorHandling;
using Microsoft.Ccr.Core;
using System;
using System.Diagnostics;
using System.Threading;

namespace MFDLabs.Concurrency
{
    /// <inheritdoc/>
    public class ConcurrencyService : CcrServiceBase, IDisposable
    {
        private readonly DispatcherMonitor _Monitor;

        /// <inheritdoc/>
        public new DispatcherQueue TaskQueue
        {
            get { return base.TaskQueue; }
        }

        /// <inheritdoc/>
        public static readonly ConcurrencyService Singleton = new ConcurrencyService(false);

        private ConcurrencyService(bool monitor)
            : base(
                  new PatchedDispatcherQueue(
                      global::MFDLabs.Concurrency.Properties.ConcurrencySettings.Default.PrimaryDispatcherQueueName,
                      new Dispatcher(
                          global::MFDLabs.Concurrency.Properties.ConcurrencySettings.Default.PrimaryDispatcherThreadCount,
                          global::MFDLabs.Concurrency.Properties.ConcurrencySettings.Default.PrimaryDispatcherThreadPriority,
                          global::MFDLabs.Concurrency.Properties.ConcurrencySettings.Default.PrimaryDispatcherOptions,
                          global::MFDLabs.Concurrency.Properties.ConcurrencySettings.Default.PrimaryDispatcherThreadPoolName
                      )
                  )
              )
        {
            if (monitor)
                _Monitor = new DispatcherMonitor(TaskQueue.Dispatcher);

            var performanceMonitor = new Thread(MonitorPerformance)
            {
                IsBackground = true,
                Name = "Performance Monitor: Concurrency Service"
            };
            performanceMonitor.Start();
        }

        private void MonitorPerformance()
        {
            if (global::MFDLabs.Concurrency.Properties.ConcurrencySettings.Default.UsePerfmon)
            {
                try
                {
                    var performanceCategory = "MFDLabs ConcurrencyService"; // TODO: Make this into a setting :)

                    if (!PerformanceCounterCategory.Exists(performanceCategory))
                    {
                        var counterData = new CounterCreationDataCollection
                        {
                            new CounterCreationData("TaskQueue Count", string.Empty, PerformanceCounterType.NumberOfItems32),
                            new CounterCreationData("TaskQueue CurrentSchedulingRate", string.Empty, PerformanceCounterType.RateOfCountsPerSecond64),
                            new CounterCreationData("TaskQueue ScheduledTaskCount", string.Empty, PerformanceCounterType.NumberOfItems64),
                            new CounterCreationData("Dispatcher PendingTaskCount", string.Empty, PerformanceCounterType.NumberOfItems32),
                            new CounterCreationData("Dispatcher ProcessedTaskCount", string.Empty, PerformanceCounterType.NumberOfItems64),
                            new CounterCreationData("Dispatcher WorkerThreadCount", string.Empty, PerformanceCounterType.NumberOfItems32)
                        };
                        PerformanceCounterCategory.Create(performanceCategory, string.Empty, PerformanceCounterCategoryType.SingleInstance, counterData);
                    }
                    var perfQueueCount = new PerformanceCounter(performanceCategory, "TaskQueue Count", false);
                    var perfCurrentSchedulingRate = new PerformanceCounter(performanceCategory, "TaskQueue CurrentSchedulingRate", false);
                    var perfScheduledTaskCount = new PerformanceCounter(performanceCategory, "TaskQueue ScheduledTaskCount", false);
                    var perfPendingTaskCount = new PerformanceCounter(performanceCategory, "Dispatcher PendingTaskCount", false);
                    var perfProcessdTaskCount = new PerformanceCounter(performanceCategory, "Dispatcher ProcessedTaskCount", false);
                    var perfWorkerThreadCount = new PerformanceCounter(performanceCategory, "Dispatcher WorkerThreadCount", false);

                    var scheduledTaskCount = TaskQueue.ScheduledTaskCount;
                    while (true)
                    {
                        perfQueueCount.RawValue = TaskQueue.Count;
                        {
                            var count = TaskQueue.ScheduledTaskCount;
                            perfCurrentSchedulingRate.IncrementBy(count - scheduledTaskCount);
                            scheduledTaskCount = count;
                        }
                        perfScheduledTaskCount.RawValue = scheduledTaskCount;
                        perfPendingTaskCount.RawValue = TaskQueue.Dispatcher.PendingTaskCount;
                        perfProcessdTaskCount.RawValue = TaskQueue.Dispatcher.ProcessedTaskCount;
                        perfWorkerThreadCount.RawValue = TaskQueue.Dispatcher.WorkerThreadCount;
                        Thread.Sleep(500);
                    }
                }
                catch (ThreadAbortException)
                {
                }
                catch (Exception ex)
                {
                    ExceptionHandler.LogException(ex);
                }
            }
        }

        /// <inheritdoc/>
        public bool BlockUntilCompletion(ITask task, TimeSpan timeout)
        {
            // TODO: Pool these handles for better performance.
            using (var handle = new EventWaitHandle(false, EventResetMode.ManualReset))
            {
                var donePort = new Port<EmptyValue>();
                Arbiter.ExecuteToCompletion(TaskQueue, task, donePort);
                Activate(
                    Arbiter.Receive(
                        false,
                        donePort,
                        (e) => handle.Set()
                    )
                );
                return handle.WaitOne(timeout);
            }
        }

        /// <inheritdoc/>
        public SuccessFailurePort Choice(Action<SuccessResult> successHandler, Action<Exception> failureHandler)
        {
            var result = new SuccessFailurePort();
            Choice(result, successHandler, failureHandler);
            return result;
        }

        /// <inheritdoc/>
        public PortSet<T0, T1> Choice<T0, T1>(Action<T0> handler0, Action<T1> handler1)
        {
            var result = new PortSet<T0, T1>();
            Choice(result, handler0, handler1);
            return result;
        }

        /// <inheritdoc/>
        public void Choice<T0, T1>(PortSet<T0, T1> resultPortSet, Action<T0> handler0, Action<T1> handler1)
        {
            var choice = Arbiter.Choice(
                resultPortSet,
                (result0) =>
                {
                    try
                    {
                        handler0(result0);
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandler.LogException(ex);
                    }
                },
                (result1) =>
                {
                    try
                    {
                        handler1(result1);
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandler.LogException(ex);
                    }
                }
            );
            Singleton.Activate(choice);
        }

        /// <inheritdoc/>
        public void Choice<T>(PortSet<T, Exception> resultPortSet, Action<T> successHandler)
        {
            Choice(
                resultPortSet,
                successHandler,
                ExceptionHandler.LogException
            );
        }

        /// <inheritdoc/>
        public void Delay(TimeSpan timeSpan, Handler handler)
        {
            var timeoutPort = TimeoutPort(timeSpan);
            var receiver = Arbiter.Receive(false, timeoutPort, (time) => handler());
            Activate(receiver);
        }

        /// <inheritdoc/>
        public void DelayInterator(TimeSpan timeSpan, IteratorHandler handler)
        {
            var timeoutPort = TimeoutPort(timeSpan);
            var receiver = Arbiter.Receive(false, timeoutPort, (time) => SpawnIterator(handler));
            Activate(receiver);
        }

        /// <inheritdoc/>
        public ITask ExecuteToCompletion(IteratorHandler handler)
        {
            // The bool ensures that causalties are propagated
            return Arbiter.ExecuteToCompletion(
                TaskQueue,
                new IterativeTask<bool>(
                    true,
                    (notUsed) => handler()
                 )
            );
        }

        /// <inheritdoc/>
        public ITask NestIterator(IteratorHandler handler)
        {
            return Arbiter.ExecuteToCompletion(
                base.TaskQueue,
                Arbiter.FromIteratorHandler(handler)
            );
        }

        /// <inheritdoc/>
        public Port<T> Receive<T>(bool persist, Action<T> handler)
        {
            var result = new Port<T>();
            Receive(persist, result, handler);
            return result;
        }

        /// <inheritdoc/>
        public void Receive<T>(bool persist, Port<T> result, Action<T> handler)
        {
            var receiver = Arbiter.Receive(
                persist,
                result,
                (t) =>
                {
                    try
                    {
                        handler(t);
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandler.LogException(ex);
                    }
                }
            );
            Activate(receiver);
        }

        /// <summary>
        /// Exposes the Spawn function for client with Causalties
        /// </summary>
        public new void Spawn(Handler handler)
        {
            var task = new Task<bool>(true, (notUsed) => handler());
            TaskQueue.Enqueue(task);
        }

        /// <summary>
        /// Exposes the SpawnIterator function for clients
        /// </summary>
        public new void SpawnIterator(IteratorHandler handler)
        {
            var iterativeTask = new IterativeTask<bool>(true, (notUsed) => handler());
            TaskQueue.Enqueue(iterativeTask);
        }

        /// <inheritdoc/>
        public new void SpawnIterator<T0>(T0 t0, IteratorHandler<T0> handler)
        {
            var iterativeTask = new IterativeTask<bool>(true, (notUsed) => handler(t0));
            TaskQueue.Enqueue(iterativeTask);
        }

        /// <inheritdoc/>
        public new void SpawnIterator<T0, T1>(T0 t0, T1 t1, IteratorHandler<T0, T1> handler)
        {
            var iterativeTask = new IterativeTask<bool>(true, (notUsed) => handler(t0, t1));
            TaskQueue.Enqueue(iterativeTask);
        }

        /// <inheritdoc/>
        public new void SpawnIterator<T0, T1, T2>(T0 t0, T1 t1, T2 t2, IteratorHandler<T0, T1, T2> handler)
        {
            var iterativeTask = new IterativeTask<bool>(true, (notUsed) => handler(t0, t1, t2));
            TaskQueue.Enqueue(iterativeTask);
        }

        /// <inheritdoc/>
        public new Port<DateTime> TimeoutPort(TimeSpan ts)
        {
            return base.TimeoutPort(ts);
        }

        /// <inheritdoc/>
        public ITask Wait(TimeSpan ts)
        {
            return base.TimeoutPort(ts).Receive();
        }


        #region IDisposable Members

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_Monitor != null)
                _Monitor.Dispose();

            TaskQueue.Dispose();

            base.TaskQueue.Dispose();
        }

        #endregion
    }
}