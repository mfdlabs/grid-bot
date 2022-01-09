using Microsoft.Ccr.Core.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Xml.Serialization;

namespace Microsoft.Ccr.Core
{
    public class DispatcherQueue : IDisposable
    {
        public bool IsDisposed
        {
            get
            {
                return _isDisposed;
            }
            set
            {
                _isDisposed = value;
            }
        }

        public bool IsSuspended
        {
            get
            {
                return _isSuspended;
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

        public bool IsUsingThreadPool
        {
            get
            {
                return _dispatcher == null;
            }
            set
            {
                if (_dispatcher != null && !value)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        [XmlIgnore]
        public Dispatcher Dispatcher
        {
            get
            {
                return _dispatcher;
            }
        }

        public int Count
        {
            get
            {
                return _taskQueue.Count;
            }
            set
            {
            }
        }

        public long ScheduledTaskCount
        {
            get
            {
                return _scheduledTaskCount;
            }
            set
            {
            }
        }

        public TaskExecutionPolicy Policy
        {
            get
            {
                return _policy;
            }
            set
            {
                _policy = value;
                if (value != TaskExecutionPolicy.Unconstrained && _watch == null)
                {
                    _watch = Stopwatch.StartNew();
                }
            }
        }

        public int MaximumQueueDepth
        {
            get
            {
                return _maximumQueueDepth;
            }
            set
            {
                _maximumQueueDepth = value;
            }
        }

        public double CurrentSchedulingRate
        {
            get
            {
                return _currentSchedulingRate;
            }
            set
            {
                _currentSchedulingRate = value;
            }
        }

        public double MaximumSchedulingRate
        {
            get
            {
                return _maximumSchedulingRate;
            }
            set
            {
                _maximumSchedulingRate = value;
            }
        }

        public double Timescale
        {
            get
            {
                return _timescale;
            }
            set
            {
                _timescale = value;
            }
        }

        [XmlIgnore]
        public Port<ITask> ExecutionPolicyNotificationPort
        {
            get
            {
                return _policyNotificationPort;
            }
            set
            {
                _policyNotificationPort = value;
            }
        }

        [XmlIgnore]
        public TimeSpan ThrottlingSleepInterval
        {
            get
            {
                return _throttlingSleepInterval;
            }
            set
            {
                _throttlingSleepInterval = value;
            }
        }

        public DispatcherQueue()
        {
            _name = "Unnamed queue using CLR Threadpool";
        }

        public DispatcherQueue(string name)
        {
            _name = name;
        }

        public DispatcherQueue(string name, Dispatcher dispatcher) : this(name, dispatcher, TaskExecutionPolicy.Unconstrained, 0, 1.0)
        {
        }

        public DispatcherQueue(string name, Dispatcher dispatcher, TaskExecutionPolicy policy, int maximumQueueDepth) : this(name, dispatcher, policy, maximumQueueDepth, 0.0)
        {
        }

        public DispatcherQueue(string name, Dispatcher dispatcher, TaskExecutionPolicy policy, double schedulingRate) : this(name, dispatcher, policy, 0, schedulingRate)
        {
        }

        private DispatcherQueue(string name, Dispatcher dispatcher, TaskExecutionPolicy policy, int maximumQueueDepth, double schedulingRate)
        {
            if ((policy == TaskExecutionPolicy.ConstrainQueueDepthDiscardTasks || policy == TaskExecutionPolicy.ConstrainQueueDepthThrottleExecution) && maximumQueueDepth <= 0)
            {
                throw new ArgumentOutOfRangeException("maximumQueueDepth");
            }
            if ((policy == TaskExecutionPolicy.ConstrainSchedulingRateDiscardTasks || policy == TaskExecutionPolicy.ConstrainSchedulingRateThrottleExecution) && schedulingRate <= 0.0)
            {
                throw new ArgumentOutOfRangeException("schedulingRate");
            }
            _dispatcher = dispatcher ?? throw new ArgumentNullException("dispatcher");
            _name = name;
            _policy = policy;
            _maximumQueueDepth = maximumQueueDepth;
            _maximumSchedulingRate = schedulingRate;
            dispatcher.AddQueue(name, this);
            if (policy != TaskExecutionPolicy.Unconstrained)
            {
                _watch = Stopwatch.StartNew();
            }
        }

        public virtual void EnqueueTimer(TimeSpan timeSpan, Port<DateTime> timerPort)
        {
            CausalityThreadContext causalityContext = Dispatcher.CloneCausalitiesFromCurrentThread();
            timeSpan = TimeSpan.FromSeconds(timeSpan.TotalSeconds * _timescale);
            DateTime dateTime = DateTime.UtcNow + timeSpan;
            TimerContext item = new TimerContext(timerPort, causalityContext, dateTime);
            bool flag = false;
            lock (_timerTable)
            {
                if (dateTime < _nextTimerExpiration)
                {
                    _nextTimerExpiration = dateTime;
                    flag = true;
                }
                if (_timerTable.ContainsKey(dateTime.Ticks))
                {
                    _timerTable[dateTime.Ticks].Add(item);
                }
                else
                {
                    List<TimerContext> list = new List<TimerContext>(1)
                    {
                        item
                    };
                    _timerTable[dateTime.Ticks] = list;
                }
            }
            if (flag)
            {
                Enqueue(new Task(delegate ()
                {
                }));
            }
        }

        internal bool CheckTimerExpirations()
        {
            if (_timerTable.Count == 0 || _isDisposed || _isSuspended)
            {
                return false;
            }
            if (DateTime.UtcNow < _nextTimerExpiration)
            {
                return true;
            }
            List<TimerContext> list = null;
            for (; ; )
            {
                LOOP_JMP_BACK:
                lock (_timerTable)
                {
                    foreach (List<TimerContext> list2 in _timerTable.Values)
                    {
                        if (list2[0].Expiration <= DateTime.UtcNow)
                        {
                            if (list == null)
                            {
                                list = new List<TimerContext>();
                            }
                            list.AddRange(list2);
                            _timerTable.Remove(list2[0].Expiration.Ticks);
                            goto LOOP_JMP_BACK;
                        }
                    }
                    if (_timerTable.Count == 0)
                    {
                        _nextTimerExpiration = DateTime.UtcNow.AddDays(1.0);
                    }
                    else
                    {
                        using (IEnumerator<List<TimerContext>> enumerator2 = _timerTable.Values.GetEnumerator())
                        {
                            if (enumerator2.MoveNext())
                            {
                                List<TimerContext> list3 = enumerator2.Current;
                                _nextTimerExpiration = list3[0].Expiration;
                            }
                        }
                    }
                }
                break;
            }
            if (list != null)
            {
                foreach (TimerContext tc in list)
                {
                    SignalTimer(tc);
                }
            }
            return true;
        }

        private void SignalTimer(TimerContext tc)
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

        internal void TaskListAddLast(TaskCommon Item)
        {
            if (_taskCommonListHead == null)
            {
                _taskCommonListHead = Item;
                Item._next = Item;
                Item._previous = Item;
            }
            else
            {
                _taskCommonListHead._previous._next = Item;
                Item._previous = _taskCommonListHead._previous;
                Item._next = _taskCommonListHead;
                _taskCommonListHead._previous = Item;
            }
            _taskCommonCount++;
        }

        internal TaskCommon TaskListRemoveFirst()
        {
            if (_taskCommonListHead == null)
            {
                return null;
            }
            if (_taskCommonListHead._next == _taskCommonListHead)
            {
                TaskCommon taskCommonListHead = _taskCommonListHead;
                _taskCommonListHead = null;
                _taskCommonCount--;
                return taskCommonListHead;
            }
            TaskCommon taskCommonListHead2 = _taskCommonListHead;
            _taskCommonListHead = _taskCommonListHead._next;
            _taskCommonListHead._previous = taskCommonListHead2._previous;
            _taskCommonListHead._previous._next = _taskCommonListHead;
            _taskCommonCount--;
            return taskCommonListHead2;
        }

        public virtual bool Enqueue(ITask task)
        {
            bool flag = true;
            bool flag2 = false;
            if (task == null)
            {
                throw new ArgumentNullException("task");
            }
            task.TaskQueue = this;
            if (_dispatcher == null)
            {
                _scheduledTaskCount += 1L;
                ThreadPool.QueueUserWorkItem(new WaitCallback(TaskExecutionWorker.ExecuteInCurrentThreadContext), task);
                return flag;
            }
            lock (_taskQueue)
            {
                if (_isDisposed)
                {
                    if ((_dispatcher.Options & DispatcherOptions.SuppressDisposeExceptions) == DispatcherOptions.None)
                    {
                        throw new ObjectDisposedException(typeof(DispatcherQueue).Name + ":" + Name);
                    }
                    return false;
                }
                else
                {
                    switch (_policy)
                    {
                        case TaskExecutionPolicy.Unconstrained:
                            {
                                if (task is TaskCommon taskCommon)
                                {
                                    TaskListAddLast(taskCommon);
                                }
                                else
                                {
                                    _taskQueue.Enqueue(task);
                                }
                                break;
                            }
                        case TaskExecutionPolicy.ConstrainQueueDepthDiscardTasks:
                            RecalculateSchedulingRate();
                            if (_taskQueue.Count >= _maximumQueueDepth)
                            {
                                Dispatcher.LogInfo("Enqueue: Discarding oldest task because queue depth limit reached");
                                TryDequeue(out ITask task2);
                                flag = false;
                            }
                            _taskQueue.Enqueue(task);
                            break;
                        case TaskExecutionPolicy.ConstrainQueueDepthThrottleExecution:
                            RecalculateSchedulingRate();
                            if (_taskQueue.Count >= _maximumQueueDepth)
                            {
                                Dispatcher.LogInfo("Enqueue: Forcing thread sleep because queue depth limit reached");
                                while (_taskQueue.Count >= _maximumQueueDepth)
                                {
                                    Sleep();
                                }
                                flag2 = true;
                            }
                            _taskQueue.Enqueue(task);
                            break;
                        case TaskExecutionPolicy.ConstrainSchedulingRateDiscardTasks:
                            RecalculateSchedulingRate();
                            if (_currentSchedulingRate >= _maximumSchedulingRate)
                            {
                                Dispatcher.LogInfo("Enqueue: Discarding task because task scheduling rate exceeded");
                                TryDequeue(out ITask task3);
                                flag = false;
                            }
                            _scheduledItems += 1.0;
                            _taskQueue.Enqueue(task);
                            break;
                        case TaskExecutionPolicy.ConstrainSchedulingRateThrottleExecution:
                            RecalculateSchedulingRate();
                            if (_currentSchedulingRate >= _maximumSchedulingRate)
                            {
                                Dispatcher.LogInfo("Enqueue: Forcing thread sleep because task scheduling rate exceeded");
                                while (_currentSchedulingRate > _maximumSchedulingRate)
                                {
                                    Sleep();
                                    RecalculateSchedulingRate();
                                }
                                flag2 = true;
                            }
                            _scheduledItems += 1.0;
                            _taskQueue.Enqueue(task);
                            break;
                    }
                    _scheduledTaskCount += 1L;
                    _dispatcher.Signal();
                }
            }
            if (!flag || flag2)
            {
                TaskExecutionPolicyEngaged(task, flag2);
            }
            return flag;
        }

        private void Sleep()
        {
            Monitor.Exit(_taskQueue);
            Thread.Sleep(_throttlingSleepInterval);
            Monitor.Enter(_taskQueue);
        }

        private void TaskExecutionPolicyEngaged(ITask task, bool throttlingEnabled)
        {
            if (!throttlingEnabled)
            {
                Interlocked.Decrement(ref _dispatcher._pendingTaskCount);
            }
            Port<ITask> policyNotificationPort = _policyNotificationPort;
            if (policyNotificationPort != null)
            {
                policyNotificationPort.Post(throttlingEnabled ? null : task);
            }
        }

        public virtual void Suspend()
        {
            lock (_taskQueue)
            {
                if (!_isSuspended)
                {
                    if (_dispatcher != null)
                    {
                        _dispatcher.QueueSuspendNotification();
                    }
                    _isSuspended = true;
                }
            }
        }

        public virtual void Resume()
        {
            lock (_taskQueue)
            {
                if (!_isSuspended)
                {
                    return;
                }
                if (_dispatcher != null)
                {
                    _dispatcher.QueueResumeNotification();
                }
                _isSuspended = false;
            }
            Enqueue(new Task(delegate ()
            {
            }));
        }

        public virtual bool TryDequeue(out ITask task)
        {
            if (_dispatcher == null)
            {
                throw new InvalidOperationException(Resource.DispatcherPortTestNotValidInThreadpoolMode);
            }
            lock (_taskQueue)
            {
                if (_isDisposed)
                {
                    task = null;
                    if ((_dispatcher.Options & DispatcherOptions.SuppressDisposeExceptions) == DispatcherOptions.None)
                    {
                        throw new ObjectDisposedException(typeof(DispatcherQueue).Name + ":" + Name);
                    }
                    return false;
                }
                else
                {
                    if (_isSuspended)
                    {
                        task = null;
                        return false;
                    }
                    if (_taskCommonCount > 0)
                    {
                        task = TaskListRemoveFirst();
                    }
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
            }
            Interlocked.Decrement(ref _dispatcher._pendingTaskCount);
            return true;
        }

        private void RecalculateSchedulingRate()
        {
            _currentSchedulingRate = _scheduledItems / _watch.Elapsed.TotalSeconds;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _isDisposed = true;
                if (_dispatcher == null)
                {
                    return;
                }
                if (_dispatcher.RemoveQueue(_name))
                {
                    lock (_taskQueue)
                    {
                        _dispatcher.AdjustPendingCount(-(_taskQueue.Count + _taskCommonCount));
                    }
                }
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

        internal bool RaiseUnhandledException(Exception exception)
        {
            if (_unhandledPort != null)
            {
                _unhandledPort.Post(exception);
            }
            UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(exception, false));
            return _unhandledPort != null || UnhandledException != null;
        }

        private string _name;

        private readonly Queue<ITask> _taskQueue = new Queue<ITask>();

        private TaskCommon _taskCommonListHead;

        private bool _isDisposed;

        private bool _isSuspended;

        internal Dispatcher _dispatcher;

        private long _scheduledTaskCount;

        private TaskExecutionPolicy _policy;

        private Stopwatch _watch;

        private int _maximumQueueDepth;

        private double _currentSchedulingRate;

        private double _scheduledItems;

        private double _maximumSchedulingRate;

        private double _timescale = 1.0;

        private Port<ITask> _policyNotificationPort;

        private TimeSpan _throttlingSleepInterval = TimeSpan.FromMilliseconds(10.0);

        private readonly SortedList<long, List<TimerContext>> _timerTable = new SortedList<long, List<TimerContext>>();

        private DateTime _nextTimerExpiration = DateTime.UtcNow + TimeSpan.FromDays(1.0);

        private int _taskCommonCount;

        private Port<Exception> _unhandledPort;

        private class TimerContext
        {
            public TimerContext(Port<DateTime> timerPort, CausalityThreadContext causalityContext, DateTime expiration)
            {
                CausalityContext = causalityContext;
                TimerPort = timerPort;
                Expiration = expiration;
            }

            public Port<DateTime> TimerPort;

            public CausalityThreadContext CausalityContext;

            public DateTime Expiration;
        }
    }
}
