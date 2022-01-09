using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Xml.Serialization;
using Microsoft.Ccr.Core.Properties;

namespace Microsoft.Ccr.Core
{
    public class DispatcherQueue : IDisposable
    {
        public bool IsDisposed { get; set; }
        public bool IsSuspended { get; private set; }
        public string Name { get; set; }
        public bool IsUsingThreadPool
        {
            get => _dispatcher == null;
            set
            {
                if (_dispatcher != null && !value)
                    throw new InvalidOperationException();
            }
        }
        [XmlIgnore]
        public Dispatcher Dispatcher => _dispatcher;
        public int Count
        {
            get => _taskQueue.Count;
            set { }
        }
        public long ScheduledTaskCount
        {
            get => _scheduledTaskCount;
            set { }
        }
        public TaskExecutionPolicy Policy
        {
            get => _policy;
            set
            {
                _policy = value;
                if (value != TaskExecutionPolicy.Unconstrained && _watch == null) 
                    _watch = Stopwatch.StartNew();
            }
        }
        public int MaximumQueueDepth { get; set; }
        public double CurrentSchedulingRate { get; set; }
        public double MaximumSchedulingRate { get; set; }
        public double Timescale { get; set; } = 1.0;
        [XmlIgnore]
        public Port<ITask> ExecutionPolicyNotificationPort { get; set; }
        [XmlIgnore]
        public TimeSpan ThrottlingSleepInterval { get; set; } = TimeSpan.FromMilliseconds(10.0);

        public DispatcherQueue() => Name = "Unnamed queue using CLR Threadpool";
        public DispatcherQueue(string name) => Name = name;
        public DispatcherQueue(string name, Dispatcher dispatcher) 
            : this(name, dispatcher, TaskExecutionPolicy.Unconstrained, 0, 1.0)
        {}
        public DispatcherQueue(string name, Dispatcher dispatcher, TaskExecutionPolicy policy, int maximumQueueDepth) 
            : this(name, dispatcher, policy, maximumQueueDepth, 0.0)
        {}
        public DispatcherQueue(string name, Dispatcher dispatcher, TaskExecutionPolicy policy, double schedulingRate) 
            : this(name, dispatcher, policy, 0, schedulingRate)
        {}
        private DispatcherQueue(string name,
            Dispatcher dispatcher,
            TaskExecutionPolicy policy,
            int maximumQueueDepth,
            double schedulingRate)
        {
            switch (policy)
            {
                case TaskExecutionPolicy.ConstrainQueueDepthDiscardTasks
                    or TaskExecutionPolicy.ConstrainQueueDepthThrottleExecution when maximumQueueDepth <= 0:
                    throw new ArgumentOutOfRangeException(nameof(maximumQueueDepth));
                case TaskExecutionPolicy.ConstrainSchedulingRateDiscardTasks
                    or TaskExecutionPolicy.ConstrainSchedulingRateThrottleExecution when schedulingRate <= 0.0:
                    throw new ArgumentOutOfRangeException(nameof(schedulingRate));
            }

            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            Name = name;
            _policy = policy;
            MaximumQueueDepth = maximumQueueDepth;
            MaximumSchedulingRate = schedulingRate;
            dispatcher.AddQueue(name, this);
            if (policy != TaskExecutionPolicy.Unconstrained) _watch = Stopwatch.StartNew();
        }

        public virtual void EnqueueTimer(TimeSpan timeSpan, Port<DateTime> timerPort)
        {
            var ctx = Dispatcher.CloneCausalitiesFromCurrentThread();
            timeSpan = TimeSpan.FromSeconds(timeSpan.TotalSeconds * Timescale);
            var dt = DateTime.UtcNow + timeSpan;
            var tCtx = new TimerContext(timerPort, ctx, dt);
            var enQ = false;
            lock (_timerTable)
            {
                if (dt < _nextTimerExpiration)
                {
                    _nextTimerExpiration = dt;
                    enQ = true;
                }
                if (_timerTable.ContainsKey(dt.Ticks)) _timerTable[dt.Ticks].Add(tCtx);
                else
                {
                    var ctxL = new List<TimerContext>(1) { tCtx };
                    _timerTable[dt.Ticks] = ctxL;
                }
            }
            if (enQ) Enqueue(new Task(() => {}));
        }
        internal bool CheckTimerExpirations()
        {
            if (_timerTable.Count == 0 || IsDisposed || IsSuspended) return false;
            if (DateTime.UtcNow < _nextTimerExpiration) return true;
            List<TimerContext> ctxList = null;
            while (true)
            {
                LOOP_JMP_BACK:
                lock (_timerTable)
                {
                    foreach (var timerCtxTable in _timerTable.Values)
                    {
                        if (timerCtxTable[0].Expiration > DateTime.UtcNow) continue;
                        ctxList ??= new List<TimerContext>();
                        ctxList.AddRange(timerCtxTable);
                        _timerTable.Remove(timerCtxTable[0].Expiration.Ticks);
                        goto LOOP_JMP_BACK;
                    }
                    if (_timerTable.Count == 0) 
                        _nextTimerExpiration = DateTime.UtcNow.AddDays(1.0);
                    else
                    {
                        using var en = _timerTable.Values.GetEnumerator();
                        if (en.MoveNext()) _nextTimerExpiration = en.Current[0].Expiration;
                    }
                }
                break;
            }

            if (ctxList == null) return true;
            foreach (var tc in ctxList) SignalTimer(tc);
            return true;
        }
        private static void SignalTimer(TimerContext tc)
        {
            try
            {
                Dispatcher.SetCurrentThreadCausalities(tc.CausalityContext);
                tc.TimerPort.Post(DateTime.Now);
                Dispatcher.ClearCausalities();
            }
            catch (Exception exception)
            {
                Dispatcher.LogError("DispatcherQueue:TimerHandler", exception);
            }
        }
        internal void TaskListAddLast(TaskCommon item)
        {
            if (_taskCommonListHead == null)
            {
                _taskCommonListHead = item;
                item._next = item;
                item._previous = item;
            }
            else
            {
                _taskCommonListHead._previous._next = item;
                item._previous = _taskCommonListHead._previous;
                item._next = _taskCommonListHead;
                _taskCommonListHead._previous = item;
            }
            _taskCommonCount++;
        }
        internal TaskCommon TaskListRemoveFirst()
        {
            if (_taskCommonListHead == null) return null;
            if (_taskCommonListHead._next == _taskCommonListHead)
            {
                var l1 = _taskCommonListHead;
                _taskCommonListHead = null;
                _taskCommonCount--;
                return l1;
            }
            var l2 = _taskCommonListHead;
            _taskCommonListHead = _taskCommonListHead._next;
            _taskCommonListHead._previous = l2._previous;
            _taskCommonListHead._previous._next = _taskCommonListHead;
            _taskCommonCount--;
            return l2;
        }
        public virtual bool Enqueue(ITask task)
        {
            var overflowed = true;
            var underflowed = false;
            if (task == null) throw new ArgumentNullException(nameof(task));
            task.TaskQueue = this;
            if (_dispatcher == null)
            {
                _scheduledTaskCount += 1;
                ThreadPool.QueueUserWorkItem(TaskExecutionWorker.ExecuteInCurrentThreadContext, task);
                return true;
            }
            lock (_taskQueue)
            {
                if (IsDisposed)
                {
                    if ((_dispatcher.Options & DispatcherOptions.SuppressDisposeExceptions) == DispatcherOptions.None) 
                        throw new ObjectDisposedException(nameof(DispatcherQueue) + ":" + Name);
                    return false;
                }

                switch (_policy)
                {
                    case TaskExecutionPolicy.Unconstrained:
                    {
                        if (task is TaskCommon taskCommon) 
                            TaskListAddLast(taskCommon);
                        else 
                            _taskQueue.Enqueue(task);
                        break;
                    }
                    case TaskExecutionPolicy.ConstrainQueueDepthDiscardTasks:
                        RecalculateSchedulingRate();
                        if (_taskQueue.Count >= MaximumQueueDepth)
                        {
                            Dispatcher.LogInfo("Enqueue: Discarding oldest task because queue depth limit reached");
                            TryDequeue(out _);
                            overflowed = false;
                        }
                        _taskQueue.Enqueue(task);
                        break;
                    case TaskExecutionPolicy.ConstrainQueueDepthThrottleExecution:
                        RecalculateSchedulingRate();
                        if (_taskQueue.Count >= MaximumQueueDepth)
                        {
                            Dispatcher.LogInfo("Enqueue: Forcing thread sleep because queue depth limit reached");
                            while (_taskQueue.Count >= MaximumQueueDepth) 
                                Sleep();
                            underflowed = true;
                        }
                        _taskQueue.Enqueue(task);
                        break;
                    case TaskExecutionPolicy.ConstrainSchedulingRateDiscardTasks:
                        RecalculateSchedulingRate();
                        if (CurrentSchedulingRate >= MaximumSchedulingRate)
                        {
                            Dispatcher.LogInfo("Enqueue: Discarding task because task scheduling rate exceeded");
                            TryDequeue(out _);
                            overflowed = false;
                        }
                        _scheduledItems += 1;
                        _taskQueue.Enqueue(task);
                        break;
                    case TaskExecutionPolicy.ConstrainSchedulingRateThrottleExecution:
                        RecalculateSchedulingRate();
                        if (CurrentSchedulingRate >= MaximumSchedulingRate)
                        {
                            Dispatcher.LogInfo("Enqueue: Forcing thread sleep because task scheduling rate exceeded");
                            while (CurrentSchedulingRate > MaximumSchedulingRate)
                            {
                                Sleep();
                                RecalculateSchedulingRate();
                            }
                            underflowed = true;
                        }
                        _scheduledItems += 1;
                        _taskQueue.Enqueue(task);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                _scheduledTaskCount += 1;
                _dispatcher.Signal();
            }
            if (!overflowed || underflowed) 
                TaskExecutionPolicyEngaged(task, underflowed);
            return overflowed;
        }
        private void Sleep()
        {
            Monitor.Exit(_taskQueue);
            Thread.Sleep(ThrottlingSleepInterval);
            Monitor.Enter(_taskQueue);
        }
        private void TaskExecutionPolicyEngaged(ITask task, bool throttlingEnabled)
        {
            if (!throttlingEnabled) Interlocked.Decrement(ref _dispatcher._pendingTaskCount);
            var policyNotificationPort = ExecutionPolicyNotificationPort;
            policyNotificationPort?.Post(throttlingEnabled ? null : task);
        }
        public virtual void Suspend()
        {
            lock (_taskQueue)
            {
                if (IsSuspended) return;
                _dispatcher?.QueueSuspendNotification();
                IsSuspended = true;
            }
        }
        public virtual void Resume()
        {
            lock (_taskQueue)
            {
                if (!IsSuspended) return;

                _dispatcher?.QueueResumeNotification();
                IsSuspended = false;
            }
            Enqueue(new Task(() => {}));
        }
        public virtual bool TryDequeue(out ITask task)
        {
            if (_dispatcher == null) 
                throw new InvalidOperationException(Resource.DispatcherPortTestNotValidInThreadpoolMode);
            lock (_taskQueue)
            {
                if (IsDisposed)
                {
                    task = null;
                    if ((_dispatcher.Options & DispatcherOptions.SuppressDisposeExceptions) == DispatcherOptions.None) 
                        throw new ObjectDisposedException(nameof(DispatcherQueue) + ":" + Name);
                    return false;
                }

                if (IsSuspended)
                {
                    task = null;
                    return false;
                }
                if (_taskCommonCount > 0) 
                    task = TaskListRemoveFirst();
                else
                {
                    task = null;
                    if (_taskQueue.Count <= 0)
                    {
                        task = null;
                        return false;
                    }
                    task = _taskQueue.Dequeue();
                }
            }
            Interlocked.Decrement(ref _dispatcher._pendingTaskCount);
            return true;
        }
        private void RecalculateSchedulingRate() => CurrentSchedulingRate = _scheduledItems / _watch.Elapsed.TotalSeconds;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            IsDisposed = true;
            if (_dispatcher == null)
            {
                return;
            }

            if (!_dispatcher.RemoveQueue(Name)) return;
            lock (_taskQueue)
            {
                _dispatcher.AdjustPendingCount(-(_taskQueue.Count + _taskCommonCount));
            }
        }
        
        public Port<Exception> UnhandledExceptionPort { get; set; }

        public event UnhandledExceptionEventHandler UnhandledException;

        internal bool RaiseUnhandledException(Exception exception)
        {
            UnhandledExceptionPort?.Post(exception);
            UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(exception, false));
            return UnhandledExceptionPort != null || UnhandledException != null;
        }

        private readonly Queue<ITask> _taskQueue = new();
        private TaskCommon _taskCommonListHead;
        internal readonly Dispatcher _dispatcher;
        private long _scheduledTaskCount;
        private TaskExecutionPolicy _policy;
        private Stopwatch _watch;
        private double _scheduledItems;
        private readonly SortedList<long, List<TimerContext>> _timerTable = new();
        private DateTime _nextTimerExpiration = DateTime.UtcNow + TimeSpan.FromDays(1);
        private int _taskCommonCount;

        private class TimerContext
        {
            public TimerContext(Port<DateTime> timerPort, CausalityThreadContext causalityContext, DateTime expiration)
            {
                CausalityContext = causalityContext;
                TimerPort = timerPort;
                Expiration = expiration;
            }

            public readonly Port<DateTime> TimerPort;
            public readonly CausalityThreadContext CausalityContext;
            public DateTime Expiration;
        }
    }
}
