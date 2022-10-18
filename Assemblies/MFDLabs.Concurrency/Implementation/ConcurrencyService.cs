using System;
using System.Threading;
using MFDLabs.Logging;
using Microsoft.Ccr.Core;

#if NETFRAMEWORK
using System.Diagnostics;
#endif

// ReSharper disable AccessToDisposedClosure
// ReSharper disable once CheckNamespace

namespace MFDLabs.Concurrency
{
    /// <summary>
    /// A simple service class that extends the <see cref="CcrServiceBase"/>
    /// </summary>
    public class ConcurrencyService : CcrServiceBase, IDisposable
    {
        private readonly DispatcherMonitor _monitor;

        /// <summary>
        /// Exposes the base TaskQueue
        /// </summary>
        private new DispatcherQueue TaskQueue => base.TaskQueue;

        /// <summary>
        /// Exposes a singleton
        /// </summary>
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
                _monitor = new DispatcherMonitor(TaskQueue.Dispatcher);

            var performanceMonitor = new Thread(MonitorPerformance)
            {
                IsBackground = true,
                Name = "Performance Monitor: Concurrency Service"
            };
            performanceMonitor.Start();
        }

        private void MonitorPerformance()
        {
#if NETFRAMEWORK
            if (!global::MFDLabs.Concurrency.Properties.ConcurrencySettings.Default.UsePerfmon) return;
            try
            {
                const string performanceCategory = "MFDLabs ConcurrencyService"; // TODO: Make this into a setting :)

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
#endif
        }

        /// <summary>
        /// Blocks the current thread until the given task finishes
        /// </summary>
        /// <param name="task">A task to block</param>
        /// <param name="timeout">A timeout</param>
        /// <returns>Boolean</returns>
        public bool BlockUntilCompletion(ITask task, TimeSpan timeout)
        {
            // TODO: Pool these handles for better performance.
            using var handle = new EventWaitHandle(false, EventResetMode.ManualReset);
            var donePort = new Port<EmptyValue>();
            Arbiter.ExecuteToCompletion(TaskQueue, task, donePort);
            Activate(
                Arbiter.Receive(
                    false,
                    donePort,
                    _ => handle.Set()
                )
            );
            return handle.WaitOne(timeout);
        }

        /// <summary>
        /// Performs a Choice operation
        /// </summary>
        /// <param name="successHandler">The success result</param>
        /// <param name="failureHandler">The failure result</param>
        /// <returns></returns>
        public SuccessFailurePort Choice(Action<SuccessResult> successHandler, Action<Exception> failureHandler)
        {
            var result = new SuccessFailurePort();
            Choice(result, successHandler, failureHandler);
            return result;
        }

        /// <summary>
        /// Performs generic Choice operation
        /// </summary>
        /// <param name="handler0">The success handler</param>
        /// <param name="handler1">The failure handler</param>
        /// <typeparam name="T0">SuccessResult</typeparam>
        /// <typeparam name="T1">FailureResult</typeparam>
        /// <returns></returns>
        public PortSet<T0, T1> Choice<T0, T1>(Action<T0> handler0, Action<T1> handler1)
        {
            var result = new PortSet<T0, T1>();
            Choice(result, handler0, handler1);
            return result;
        }

        /// <summary>
        /// Performs a generic choice with result
        /// </summary>
        /// <param name="resultPortSet">The result</param>
        /// <param name="handler0">The success handler</param>
        /// <param name="handler1">The failure handler</param>
        /// <typeparam name="T0">SuccessResult</typeparam>
        /// <typeparam name="T1">FailureResult</typeparam>
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
                        Logger.Singleton.Error(ex);
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
                        Logger.Singleton.Error(ex);
                    }
                }
            );
            Singleton.Activate(choice);
        }

        /// <summary>
        /// Perfoms a generic choice with only success T
        /// </summary>
        /// <param name="resultPortSet">The result</param>
        /// <param name="successHandler">On success hit.</param>
        /// <typeparam name="T">SuccessResult</typeparam>
        public void Choice<T>(PortSet<T, Exception> resultPortSet, Action<T> successHandler)
        {
            Choice(
                resultPortSet,
                successHandler,
                Logger.Singleton.Error
            );
        }

        /// <summary>
        /// Delay for the given timespan and then execute the Handler
        /// </summary>
        /// <param name="timeSpan">Time to delay</param>
        /// <param name="handler">Handler to hit</param>
        public void Delay(TimeSpan timeSpan, Handler handler)
        {
            var timeoutPort = TimeoutPort(timeSpan);
            var receiver = Arbiter.Receive(false, timeoutPort, _ => handler());
            Activate(receiver);
        }

        /// <summary>
        /// Delay an iterator task
        /// </summary>
        /// <param name="timeSpan">Time to delay</param>
        /// <param name="handler">Handler to hit</param>
        public void DelayInterator(TimeSpan timeSpan, IteratorHandler handler)
        {
            var timeoutPort = TimeoutPort(timeSpan);
            var receiver = Arbiter.Receive(false, timeoutPort, _ => SpawnIterator(handler));
            Activate(receiver);
        }

        /// <summary>
        /// Executes an iterator until it completes
        /// </summary>
        /// <param name="handler">The handler</param>
        /// <returns></returns>
        public ITask ExecuteToCompletion(IteratorHandler handler)
        {
            // The bool ensures that causalties are propagated
            return Arbiter.ExecuteToCompletion(
                TaskQueue,
                new IterativeTask<bool>(
                    true,
                    _ => handler()
                 )
            );
        }

        /// <summary>
        /// Nest an iterator handler
        /// </summary>
        /// <param name="handler">The handler</param>
        /// <returns></returns>
        public ITask NestIterator(IteratorHandler handler)
        {
            return Arbiter.ExecuteToCompletion(
                base.TaskQueue,
                Arbiter.FromIteratorHandler(handler)
            );
        }

        /// <summary>
        /// Init a receive on a new Port
        /// </summary>
        /// <param name="persist">Persist receives</param>
        /// <param name="handler">On Receive</param>
        /// <typeparam name="T">SuccessResult</typeparam>
        /// <returns></returns>
        public Port<T> Receive<T>(bool persist, Action<T> handler)
        {
            var result = new Port<T>();
            Receive(persist, result, handler);
            return result;
        }

        /// <summary>
        /// Generic receive on a result port
        /// </summary>
        /// <param name="persist">Persist receives</param>
        /// <param name="result">Result port</param>
        /// <param name="handler">On Receive</param>
        /// <typeparam name="T">SuccessResult</typeparam>
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
                        Logger.Singleton.Error(ex);
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
            var task = new Task<bool>(true, _ => handler());
            TaskQueue.Enqueue(task);
        }

        /// <summary>
        /// Exposes the SpawnIterator function for clients
        /// </summary>
        public new void SpawnIterator(IteratorHandler handler)
        {
            var iterativeTask = new IterativeTask<bool>(true, _ => handler());
            TaskQueue.Enqueue(iterativeTask);
        }

        /// <summary>
        /// Spawn an iterator for the given handler
        /// </summary>
        /// <param name="t0">SuccessResult</param>
        /// <param name="handler">OnIterator</param>
        /// <typeparam name="T0">SuccessResult</typeparam>
        public new void SpawnIterator<T0>(T0 t0, IteratorHandler<T0> handler)
        {
            var iterativeTask = new IterativeTask<bool>(true, _ => handler(t0));
            TaskQueue.Enqueue(iterativeTask);
        }

        /// <summary>
        /// Spawn an iterator for the given handler
        /// </summary>
        /// <param name="t0">SuccessResult</param>
        /// <param name="t1">FailureResult</param>
        /// <param name="handler">OnIterator</param>
        /// <typeparam name="T0">SuccessResult</typeparam>
        /// <typeparam name="T1">FailureResult</typeparam>
        public new void SpawnIterator<T0, T1>(T0 t0, T1 t1, IteratorHandler<T0, T1> handler)
        {
            var iterativeTask = new IterativeTask<bool>(true, _ => handler(t0, t1));
            TaskQueue.Enqueue(iterativeTask);
        }

        /// <summary>
        /// Spawn an iterator for the given handler
        /// </summary>
        /// <param name="t0">SuccessResult</param>
        /// <param name="t1">FailureResult</param>
        /// <param name="t2"></param>
        /// <param name="handler">OnIterator</param>
        /// <typeparam name="T0">SuccessResult</typeparam>
        /// <typeparam name="T1">FailureResult</typeparam>
        /// <typeparam name="T2"></typeparam>
        public new void SpawnIterator<T0, T1, T2>(T0 t0, T1 t1, T2 t2, IteratorHandler<T0, T1, T2> handler)
        {
            var iterativeTask = new IterativeTask<bool>(true, _ => handler(t0, t1, t2));
            TaskQueue.Enqueue(iterativeTask);
        }

        /// <summary>
        /// Exposes base TimeoutPort
        /// </summary>
        /// <param name="ts"></param>
        /// <returns></returns>
        public new Port<DateTime> TimeoutPort(TimeSpan ts)
        {
            return base.TimeoutPort(ts);
        }

        /// <summary>
        /// Wait for receive
        /// </summary>
        /// <param name="ts"></param>
        /// <returns></returns>
        public ITask Wait(TimeSpan ts)
        {
            return base.TimeoutPort(ts).Receive();
        }


        #region IDisposable Members

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_monitor != null)
                _monitor.Dispose();

            TaskQueue.Dispose();

            base.TaskQueue.Dispose();
        }

        #endregion
    }
}