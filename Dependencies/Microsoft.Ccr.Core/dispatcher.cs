using Microsoft.Ccr.Core.Arbiters;
using Microsoft.Ccr.Core.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace Microsoft.Ccr.Core
{
    public sealed class Dispatcher : IDisposable
    {
        internal static void AddThread(Thread thread)
        {
            lock (_causalityTable)
            {
                _causalityTable[thread.ManagedThreadId] = null;
            }
        }

        internal static void SetCurrentThreadCausalities(CausalityThreadContext context)
        {
            if (!_causalitiesActive)
            {
                return;
            }
            try
            {
                _causalityTable[Thread.CurrentThread.ManagedThreadId] = context;
            }
            catch (Exception)
            {
                if (!_causalityTable.ContainsKey(Thread.CurrentThread.ManagedThreadId))
                {
                    try
                    {
                        AddThread(Thread.CurrentThread);
                        _causalityTable[Thread.CurrentThread.ManagedThreadId] = context;
                    }
                    catch
                    {
                    }
                }
            }
        }

        internal static CausalityThreadContext CloneCausalitiesFromCurrentThread()
        {
            if (!_causalitiesActive)
            {
                return null;
            }
            CausalityThreadContext currentThreadCausalities = GetCurrentThreadCausalities();
            if (CausalityThreadContext.IsEmpty(currentThreadCausalities))
            {
                return null;
            }
            return currentThreadCausalities.Clone();
        }

        internal static CausalityThreadContext GetCurrentThreadCausalities()
        {
            if (!_causalitiesActive)
            {
                return null;
            }
            _causalityTable.TryGetValue(Thread.CurrentThread.ManagedThreadId, out CausalityThreadContext result);
            return result;
        }

        public static void AddCausality(ICausality causality)
        {
            _causalitiesActive = true;
            CausalityThreadContext causalityThreadContext = GetCurrentThreadCausalities();
            if (CausalityThreadContext.IsEmpty(causalityThreadContext))
            {
                causalityThreadContext = new CausalityThreadContext(causality, null);
                SetCurrentThreadCausalities(causalityThreadContext);
                return;
            }
            causalityThreadContext.AddCausality(causality);
        }

        public static void AddCausalityBreak()
        {
            AddCausality(new Causality("BreakingCausality")
            {
                BreakOnReceive = true
            });
        }

        public static bool RemoveCausality(ICausality causality)
        {
            return RemoveCausality(null, causality);
        }

        public static ICollection<ICausality> ActiveCausalities
        {
            get
            {
                CausalityThreadContext currentThreadCausalities = GetCurrentThreadCausalities();
                if (CausalityThreadContext.IsEmpty(currentThreadCausalities))
                {
                    return new Causality[0];
                }
                return currentThreadCausalities.Causalities;
            }
        }

        public static bool HasActiveCausalities
        {
            get
            {
                CausalityThreadContext currentThreadCausalities = GetCurrentThreadCausalities();
                return !CausalityThreadContext.IsEmpty(currentThreadCausalities);
            }
        }

        public static void ClearCausalities()
        {
            SetCurrentThreadCausalities(null);
        }

        public static bool RemoveCausality(string name)
        {
            return RemoveCausality(name, null);
        }

        private static bool RemoveCausality(string name, ICausality causality)
        {
            CausalityThreadContext currentThreadCausalities = GetCurrentThreadCausalities();
            return !CausalityThreadContext.IsEmpty(currentThreadCausalities) && currentThreadCausalities.RemoveCausality(name, causality);
        }

        internal static void TransferCausalitiesFromTaskToCurrentThread(ITask currentTask)
        {
            if (!_causalitiesActive)
            {
                return;
            }
            CausalityThreadContext causalityThreadContext = null;
            for (int i = 0; i < currentTask.PortElementCount; i++)
            {
                IPortElement portElement = currentTask[i];
                if (portElement != null && portElement.CausalityContext != null)
                {
                    CausalityThreadContext context = (CausalityThreadContext)portElement.CausalityContext;
                    if (causalityThreadContext == null)
                    {
                        causalityThreadContext = new CausalityThreadContext(null, null);
                    }
                    causalityThreadContext.MergeWith(context);
                }
            }
            SetCurrentThreadCausalities(causalityThreadContext);
        }

        internal static void FilterExceptionThroughCausalities(ITask task, Exception exception)
        {
            try
            {
                CausalityThreadContext currentThreadCausalities = GetCurrentThreadCausalities();
                if (CausalityThreadContext.IsEmpty(currentThreadCausalities))
                {
                    if (task != null)
                    {
                        DispatcherQueue taskQueue = task.TaskQueue;
                        if (!taskQueue.RaiseUnhandledException(exception))
                        {
                            Dispatcher dispatcher = taskQueue.Dispatcher;
                            if (dispatcher != null)
                            {
                                dispatcher.RaiseUnhandledException(exception);
                            }
                        }
                    }
                }
                else
                {
                    currentThreadCausalities.PostException(exception);
                }
            }
            catch (Exception exception2)
            {
                LogError(Resource1.ExceptionDuringCausalityHandling, exception2);
            }
        }

        private static int GetNumberOfProcessors()
        {
            int result = 1;
            try
            {
                if (TraceSwitchCore.TraceInfo)
                {
                    Trace.WriteLine("CCR Dispatcher: Processors:" + Environment.ProcessorCount);
                }
                result = Environment.ProcessorCount;
            }
            catch (Exception arg)
            {
                Trace.WriteLine("CCR Dispatcher: Exception reading processor count:" + arg);
            }
            return result;
        }

        public int PendingTaskCount
        {
            get
            {
                return _pendingTaskCount;
            }
            set
            {
            }
        }

        public long ProcessedTaskCount
        {
            get
            {
                long num = 0L;
                lock (_nameToQueueTable)
                {
                    for (int i = 0; i < _dispatcherQueues.Count; i++)
                    {
                        num += _dispatcherQueues[i].ScheduledTaskCount;
                    }
                }
                return num;
            }
            set
            {
            }
        }

        public int WorkerThreadCount
        {
            get
            {
                return _workerCount;
            }
            set
            {
            }
        }

        public static int ThreadsPerCpu
        {
            get
            {
                return _threadsPerCpu;
            }
            set
            {
                _threadsPerCpu = value;
            }
        }

        public DispatcherOptions Options
        {
            get
            {
                return _options;
            }
            set
            {
                _options = value;
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        public Port<Exception> UnhandledExceptionPort
        {
            get
            {
                return _unhandledPort;
            }
            set
            {
                _unhandledPort = value;
            }
        }

        public event UnhandledExceptionEventHandler UnhandledException;

        private void RaiseUnhandledException(Exception e)
        {
            if (_unhandledPort != null)
            {
                _unhandledPort.Post(e);
            }
            if (UnhandledException != null)
            {
                ThreadPool.QueueUserWorkItem(delegate (object state)
                {
                    UnhandledException(this, new UnhandledExceptionEventArgs(state as Exception, false));
                }, e);
            }
        }

        public Dispatcher() : this(0, null)
        {
        }

        public Dispatcher(int threadCount, string threadPoolName) : this(threadCount, ThreadPriority.Normal, DispatcherOptions.None, ApartmentState.Unknown, threadPoolName)
        {
        }

        public Dispatcher(int threadCount, ThreadPriority priority, bool useBackgroundThreads, string threadPoolName) : this(threadCount, priority, useBackgroundThreads ? DispatcherOptions.UseBackgroundThreads : DispatcherOptions.None, ApartmentState.Unknown, threadPoolName)
        {
        }

        public Dispatcher(int threadCount, ThreadPriority priority, DispatcherOptions options, string threadPoolName) : this(threadCount, priority, options, ApartmentState.Unknown, 0, threadPoolName)
        {
        }

        public Dispatcher(int threadCount, ThreadPriority priority, DispatcherOptions options, ApartmentState threadApartmentState, string threadPoolName) : this(threadCount, priority, options, threadApartmentState, 0, threadPoolName)
        {
        }

        public Dispatcher(int threadCount, ThreadPriority priority, DispatcherOptions options, ApartmentState threadApartmentState, int maxThreadStackSize, string threadPoolName)
        {
            if (threadCount == 0)
            {
                threadCount = Math.Max(NumberOfProcessorsInternal, 2) * ThreadsPerCpu;
            }
            else if (threadCount < 0)
            {
                throw new ArgumentException("Cannot create a negative number of threads. Pass 0 to use default.", "threadCount");
            }
            if (threadPoolName == null)
            {
                _name = string.Empty;
            }
            else
            {
                _name = threadPoolName;
            }
            _options = options;
            for (int i = 0; i < threadCount; i++)
            {
                AddWorker(priority, threadApartmentState, maxThreadStackSize);
            }
            StartWorkers();
        }

        private static void SetWorkerThreadAffinity(DateTime dispatcherStartTime)
        {
            try
            {
                int num = 0;
                TimeSpan t = TimeSpan.FromMilliseconds(100.0);
                foreach (object obj in Process.GetCurrentProcess().Threads)
                {
                    ProcessThread processThread = (ProcessThread)obj;
                    if (!(processThread.StartTime - dispatcherStartTime > t) && !(processThread.StartTime < dispatcherStartTime) && !(processThread.TotalProcessorTime > t))
                    {
                        IntPtr processorAffinity = new IntPtr(1 << num++ % NumberOfProcessorsInternal);
                        processThread.ProcessorAffinity = processorAffinity;
                    }
                }
            }
            catch (Exception exception)
            {
                LogError("Could not set thread affinity", exception);
            }
        }

        internal void AddQueue(string queueName, DispatcherQueue queue)
        {
            lock (_nameToQueueTable)
            {
                _nameToQueueTable.Add(queueName, queue);
                _dispatcherQueues.Add(queue);
                _cachedDispatcherQueueCount++;
            }
        }

        internal bool RemoveQueue(string queueName)
        {
            bool result;
            lock (_nameToQueueTable)
            {
                if (!_nameToQueueTable.TryGetValue(queueName, out DispatcherQueue item))
                {
                    result = false;
                }
                else
                {
                    _nameToQueueTable.Remove(queueName);
                    _dispatcherQueues.Remove(item);
                    _cachedDispatcherQueueCount--;
                    if (_dispatcherQueues.Count == 0)
                    {
                        Dispose();
                    }
                    result = true;
                }
            }
            return result;
        }

        public List<DispatcherQueue> DispatcherQueues
        {
            get
            {
                return new List<DispatcherQueue>(_nameToQueueTable.Values);
            }
        }

        private void AddWorker(ThreadPriority priority, ApartmentState apartmentState, int maxThreadStackSize)
        {
            TaskExecutionWorker taskExecutionWorker = new TaskExecutionWorker(this);
            Thread thread = new Thread(new ThreadStart(taskExecutionWorker.ExecutionLoop), maxThreadStackSize);
            thread.SetApartmentState(apartmentState);
            thread.Name = _name;
            thread.Priority = priority;
            thread.IsBackground = (DispatcherOptions.None < (_options & DispatcherOptions.UseBackgroundThreads));
            taskExecutionWorker._thread = thread;
            _taskExecutionWorkers.Add(taskExecutionWorker);
            _cachedWorkerListCount++;
        }

        private void StartWorkers()
        {
            DateTime now = DateTime.Now;
            foreach (TaskExecutionWorker taskExecutionWorker in _taskExecutionWorkers)
            {
                taskExecutionWorker._thread.Start();
            }
            _startupCompleteEvent.WaitOne();
            if ((_options & DispatcherOptions.UseProcessorAffinity) > DispatcherOptions.None)
            {
                SetWorkerThreadAffinity(now);
            }
            _startupCompleteEvent.Close();
            _startupCompleteEvent = null;
            foreach (TaskExecutionWorker taskExecutionWorker2 in _taskExecutionWorkers)
            {
                AddThread(taskExecutionWorker2._thread);
            }
        }

        internal void Signal()
        {
            if (_cachedWorkerListCount == 0)
            {
                LogError("Dispatcher disposed, will not schedule task", new ObjectDisposedException("Dispatcher"));
                return;
            }
            Interlocked.Increment(ref _pendingTaskCount);
            for (int i = 0; i < _cachedWorkerListCount; i++)
            {
                TaskExecutionWorker taskExecutionWorker = _taskExecutionWorkers[i];
                if (taskExecutionWorker.Signal())
                {
                    return;
                }
            }
        }

        internal void QueueSuspendNotification()
        {
            Interlocked.Increment(ref _suspendedQueueCount);
        }

        internal void QueueResumeNotification()
        {
            if (Interlocked.Decrement(ref _suspendedQueueCount) < 0)
            {
                throw new InvalidOperationException();
            }
        }

        public void Dispose()
        {
            if (_cachedWorkerListCount == 0)
            {
                return;
            }
            lock (_taskExecutionWorkers)
            {
                foreach (TaskExecutionWorker taskExecutionWorker in _taskExecutionWorkers)
                {
                    taskExecutionWorker.Shutdown();
                }
                _cachedWorkerListCount = 0;
            }
            if (_startupCompleteEvent != null)
            {
                _startupCompleteEvent.Close();
            }
            Shutdown(true);
        }

        private void Shutdown(bool wait)
        {
            Dispose();
            lock (_taskExecutionWorkers)
            {
                _hasShutdown = true;
                Monitor.PulseAll(_taskExecutionWorkers);
                if (wait)
                {
                    while (!_hasShutdown)
                    {
                        Monitor.Wait(_taskExecutionWorkers);
                    }
                }
            }
        }

        internal void AdjustPendingCount(int count)
        {
            Interlocked.Add(ref _pendingTaskCount, count);
        }

        internal static void LogError(string message, Exception exception)
        {
            string message2 = string.Format(CultureInfo.InvariantCulture, "*** {0}: Exception:{1}", new object[]
            {
                message,
                exception
            });
            if (TraceSwitchCore.TraceError)
            {
                Trace.WriteLine(message2);
            }
        }

        internal static void LogInfo(string message)
        {
            if (TraceSwitchCore.TraceInfo)
            {
                Trace.WriteLine("*    " + message);
            }
        }

        private const int CausalityTableMaximumSize = 1024;

        private static bool _causalitiesActive;

        private static readonly Dictionary<int, CausalityThreadContext> _causalityTable = new Dictionary<int, CausalityThreadContext>(CausalityTableMaximumSize);

        internal static readonly TraceSwitch TraceSwitchCore = new TraceSwitch("Microsoft.Ccr.Core", "Ccr.Core debug switch");

        internal int _workerCount;

        internal ManualResetEvent _startupCompleteEvent = new ManualResetEvent(false);

        internal List<DispatcherQueue> _dispatcherQueues = new List<DispatcherQueue>();

        internal int _cachedDispatcherQueueCount;

        internal List<TaskExecutionWorker> _taskExecutionWorkers = new List<TaskExecutionWorker>();

        private int _cachedWorkerListCount;

        private readonly Dictionary<string, DispatcherQueue> _nameToQueueTable = new Dictionary<string, DispatcherQueue>();

        private static int _threadsPerCpu = 1;

        private static readonly int NumberOfProcessorsInternal = GetNumberOfProcessors();

        private string _name;

        private DispatcherOptions _options;

        private Port<Exception> _unhandledPort;

        internal volatile int _pendingTaskCount;

        internal volatile int _suspendedQueueCount;

        private bool _hasShutdown;
    }
}
