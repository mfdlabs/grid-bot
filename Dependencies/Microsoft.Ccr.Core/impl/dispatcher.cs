using System;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Ccr.Core.Properties;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global


namespace Microsoft.Ccr.Core
{
    public sealed class Dispatcher : IDisposable
    {
        private static void AddThread(Thread thread)
        {
            lock (CausalityTable) 
                CausalityTable[thread.ManagedThreadId] = null;
        }
        internal static void SetCurrentThreadCausalities(CausalityThreadContext context)
        {
            if (!_causalitiesActive) return;
            try
            {
                CausalityTable[Thread.CurrentThread.ManagedThreadId] = context;
            }
            catch (Exception)
            {
                if (!CausalityTable.ContainsKey(Thread.CurrentThread.ManagedThreadId))
                {
                    try
                    {
                        AddThread(Thread.CurrentThread);
                        CausalityTable[Thread.CurrentThread.ManagedThreadId] = context;
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }
        internal static CausalityThreadContext CloneCausalitiesFromCurrentThread()
        {
            if (!_causalitiesActive) 
                return null;
            var currentCausalities = GetCurrentThreadCausalities();
            return CausalityThreadContext.IsEmpty(currentCausalities) ? null : currentCausalities.Clone();
        }
        internal static CausalityThreadContext GetCurrentThreadCausalities()
        {
            if (!_causalitiesActive) return null;
            CausalityTable.TryGetValue(Thread.CurrentThread.ManagedThreadId, out var result);
            return result;
        }
        public static void AddCausality(ICausality causality)
        {
            _causalitiesActive = true;
            var currentCausalities = GetCurrentThreadCausalities();
            if (CausalityThreadContext.IsEmpty(currentCausalities))
            {
                currentCausalities = new CausalityThreadContext(causality, null);
                SetCurrentThreadCausalities(currentCausalities);
                return;
            }
            currentCausalities.AddCausality(causality);
        }
        public static void AddCausalityBreak() => AddCausality(new Causality("BreakingCausality") { BreakOnReceive = true });
        public static bool RemoveCausality(ICausality causality) => RemoveCausality(null, causality);

        public static ICollection<ICausality> ActiveCausalities
        {
            get
            {
                var currentCausalities = GetCurrentThreadCausalities();
                return CausalityThreadContext.IsEmpty(currentCausalities)
                    ? Array.Empty<Causality>()
                    : currentCausalities.Causalities;
            }
        }
        public static bool HasActiveCausalities
        {
            get
            {
                var currentCausalities = GetCurrentThreadCausalities();
                return !CausalityThreadContext.IsEmpty(currentCausalities);
            }
        }

        public static void ClearCausalities() => SetCurrentThreadCausalities(null);
        public static bool RemoveCausality(string name) => RemoveCausality(name, null);
        private static bool RemoveCausality(string name, ICausality causality)
        {
            var currentCausalities = GetCurrentThreadCausalities();
            return !CausalityThreadContext.IsEmpty(currentCausalities) && currentCausalities.RemoveCausality(name, causality);
        }
        internal static void TransferCausalitiesFromTaskToCurrentThread(ITask currentTask)
        {
            if (!_causalitiesActive) return;
            CausalityThreadContext causalityThreadContext = null;
            for (var i = 0; i < currentTask.PortElementCount; i++)
            {
                var el = currentTask[i];
                if (el?.CausalityContext == null) continue;
                
                var context = (CausalityThreadContext)el.CausalityContext;
                causalityThreadContext ??= new CausalityThreadContext(null, null);
                causalityThreadContext.MergeWith(context);
            }
            SetCurrentThreadCausalities(causalityThreadContext);
        }
        internal static void FilterExceptionThroughCausalities(ITask task, Exception exception)
        {
            try
            {
                var currentCausalities = GetCurrentThreadCausalities();
                if (CausalityThreadContext.IsEmpty(currentCausalities))
                {
                    if (task == null) return;
                    if (task.TaskQueue.RaiseUnhandledException(exception)) return;
                    task.TaskQueue.Dispatcher?.RaiseUnhandledException(exception);
                }
                else
                    currentCausalities.PostException(exception);
            }
            catch (Exception ex)
            {
                LogError(Resource.ExceptionDuringCausalityHandling, ex);
            }
        }

        private static int GetNumberOfProcessors()
        {
            try
            {
                if (TraceSwitchCore.TraceInfo) 
                    Trace.WriteLine("CCR Dispatcher: Processors:" + Environment.ProcessorCount);
                return Environment.ProcessorCount;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("CCR Dispatcher: Exception reading processor count:" + ex);
                return 1;
            }
        }
        
        public int PendingTaskCount
        {
            get => _pendingTaskCount;
            set => throw new NotSupportedException();
        }
        public long ProcessedTaskCount
        {
            get
            {
                long taskCount = 0;
                lock (_nameToQueueTable) 
                    taskCount += _dispatcherQueues.Sum(t => t.ScheduledTaskCount);
                return taskCount;
            }
            set => throw new NotSupportedException();
        }
        public int WorkerThreadCount
        {
            get => WorkerCount;
            set => throw new NotSupportedException();
        }
        public static int ThreadsPerCpu { get; set; } = 1;
        public DispatcherOptions Options { get; set; }
        public string Name { get; set; }
        public Port<Exception> UnhandledExceptionPort { get; set; }

        public event UnhandledExceptionEventHandler UnhandledException;

        private void RaiseUnhandledException(Exception e)
        {
            UnhandledExceptionPort?.Post(e);
            if (UnhandledException != null)
            {
                ThreadPool.QueueUserWorkItem(
                    s => UnhandledException(
                        this, 
                        new UnhandledExceptionEventArgs(s as Exception, false)
                    ), 
                    e
                );
            }
        }

        public Dispatcher() 
            : this(0, null)
        {}
        public Dispatcher(int threadCount, string threadPoolName) 
            : this(threadCount, ThreadPriority.Normal, DispatcherOptions.None, ApartmentState.Unknown, threadPoolName)
        {}
        public Dispatcher(int threadCount, ThreadPriority priority, bool useBackgroundThreads, string threadPoolName) 
            : this(threadCount,
                priority,
                useBackgroundThreads
                    ? DispatcherOptions.UseBackgroundThreads
                    : DispatcherOptions.None,
                ApartmentState.Unknown,
                threadPoolName)
        {}
        public Dispatcher(int threadCount, ThreadPriority priority, DispatcherOptions options, string threadPoolName) 
            : this(threadCount, priority, options, ApartmentState.Unknown, 0, threadPoolName)
        {}
        public Dispatcher(int threadCount,
            ThreadPriority priority,
            DispatcherOptions options,
            ApartmentState threadApartmentState,
            string threadPoolName) 
            : this(threadCount, priority, options, threadApartmentState, 0, threadPoolName)
        {}
        public Dispatcher(int threadCount,
            ThreadPriority priority,
            DispatcherOptions options,
            ApartmentState threadApartmentState,
            int maxThreadStackSize,
            string threadPoolName)
        {
            threadCount = threadCount switch
            {
                0 => Math.Max(NumberOfProcessorsInternal, 2) * ThreadsPerCpu,
                < 0 => throw new ArgumentException("Cannot create a negative number of threads. Pass 0 to use default.",
                    nameof(threadCount)),
                _ => threadCount
            };
            Name = threadPoolName ?? string.Empty;
            Options = options;
            for (var i = 0; i < threadCount; i++) 
                AddWorker(priority, threadApartmentState, maxThreadStackSize);
            StartWorkers();
        }
        
        private static void SetWorkerThreadAffinity(DateTime dispatcherStartTime)
        {
#if NETFRAMEWORK // We only want to support affinity here for windows as it is obsolete on linux and not that important
            try
            {
                var affinityMask = 0;
                var t = TimeSpan.FromMilliseconds(100);
                foreach (var thread in Process.GetCurrentProcess().Threads)
                {
                    var processThread = (ProcessThread)thread;
                    if (processThread.StartTime - dispatcherStartTime > t ||
                        processThread.StartTime < dispatcherStartTime || processThread.TotalProcessorTime > t) 
                        continue;
                    
                    var processorAffinity = new IntPtr(1 << affinityMask++ % NumberOfProcessorsInternal);
                        
                    processThread.ProcessorAffinity = processorAffinity;
                }
            }
            catch (Exception exception)
            {
                LogError("Could not set thread affinity", exception);
            }
#endif
        }
        internal void AddQueue(string queueName, DispatcherQueue queue)
        {
            lock (_nameToQueueTable)
            {
                _nameToQueueTable.Add(queueName, queue);
                _dispatcherQueues.Add(queue);
                CachedDispatcherQueueCount++;
            }
        }
        internal bool RemoveQueue(string queueName)
        {
            lock (_nameToQueueTable)
            {
                if (!_nameToQueueTable.TryGetValue(queueName, out var dispatcherQ)) 
                    return false;
                _nameToQueueTable.Remove(queueName);
                _dispatcherQueues.Remove(dispatcherQ);
                CachedDispatcherQueueCount--;
                if (_dispatcherQueues.Count == 0) Dispose();
                return true;
            }
        }

        public List<DispatcherQueue> DispatcherQueues => new List<DispatcherQueue>(_nameToQueueTable.Values);

        private void AddWorker(ThreadPriority priority, ApartmentState apartmentState, int maxThreadStackSize)
        {
            var taskExecutionWorker = new TaskExecutionWorker(this);
            var thread = new Thread(taskExecutionWorker.ExecutionLoop, maxThreadStackSize);
#if !NETFRAMEWORK
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                thread.SetApartmentState(apartmentState);
#else
            thread.SetApartmentState(apartmentState);
#endif
            thread.Name = Name;
            thread.Priority = priority;
            thread.IsBackground = (DispatcherOptions.None < (Options & DispatcherOptions.UseBackgroundThreads));
            taskExecutionWorker._thread = thread;
            TaskExecutionWorkers.Add(taskExecutionWorker);
            _cachedWorkerListCount++;
        }
        private void StartWorkers()
        {
            var now = DateTime.Now;
            foreach (var worker in TaskExecutionWorkers) worker._thread.Start();
            StartupCompleteEvent.WaitOne();
            if ((Options & DispatcherOptions.UseProcessorAffinity) > DispatcherOptions.None) 
                SetWorkerThreadAffinity(now);
            StartupCompleteEvent.Close();
            StartupCompleteEvent = null;
            foreach (var worker in TaskExecutionWorkers) AddThread(worker._thread);
        }
        internal void Signal()
        {
            if (_cachedWorkerListCount == 0)
            {
                LogError("Dispatcher disposed, will not schedule task", new ObjectDisposedException("Dispatcher"));
                return;
            }
            Interlocked.Increment(ref _pendingTaskCount);
            for (var i = 0; i < _cachedWorkerListCount; i++)
            {
                var worker = TaskExecutionWorkers[i];
                if (worker.Signal()) return;
            }
        }
        internal void QueueSuspendNotification() => Interlocked.Increment(ref SuspendedQueueCount);
        internal void QueueResumeNotification()
        {
            if (Interlocked.Decrement(ref SuspendedQueueCount) < 0) 
                throw new InvalidOperationException();
        }
        public void Dispose()
        {
            if (_cachedWorkerListCount == 0) 
                return;
            lock (TaskExecutionWorkers)
            {
                foreach (var taskExecutionWorker in TaskExecutionWorkers) taskExecutionWorker.Shutdown();
                _cachedWorkerListCount = 0;
            }

            StartupCompleteEvent?.Close();
            Shutdown(true);
        }
        private void Shutdown(bool wait)
        {
            Dispose();
            lock (TaskExecutionWorkers)
            {
                _hasShutdown = true;
                Monitor.PulseAll(TaskExecutionWorkers);
                if (!wait) return;
                while (!_hasShutdown) Monitor.Wait(TaskExecutionWorkers);
            }
        }
        internal void AdjustPendingCount(int count) => Interlocked.Add(ref _pendingTaskCount, count);
        internal static void LogError(string message, Exception exception)
        {
            if (TraceSwitchCore.TraceError)
                Trace.WriteLine(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "*** {0}: Exception:{1}",
                        message,
                        exception
                    )
                );
        }
        internal static void LogInfo(string message)
        {
            if (TraceSwitchCore.TraceInfo) Trace.WriteLine("*    " + message);
        }

        private const int CausalityTableMaximumSize = 1024;

        private static bool _causalitiesActive;
        private static readonly Dictionary<int, CausalityThreadContext> CausalityTable = new(CausalityTableMaximumSize);
        internal static readonly TraceSwitch TraceSwitchCore = new("Microsoft.Ccr.Core", "Ccr.Core debug switch");
        internal int WorkerCount;
        internal ManualResetEvent StartupCompleteEvent = new(false);
        internal readonly List<DispatcherQueue> _dispatcherQueues = new();
        internal int CachedDispatcherQueueCount;
        internal readonly List<TaskExecutionWorker> TaskExecutionWorkers = new();
        private int _cachedWorkerListCount;
        private readonly Dictionary<string, DispatcherQueue> _nameToQueueTable = new();
        private static readonly int NumberOfProcessorsInternal = GetNumberOfProcessors();
        internal volatile int _pendingTaskCount;
        internal volatile int SuspendedQueueCount;
        private bool _hasShutdown;
    }
}
