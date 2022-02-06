using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Ccr.Core.Properties;

namespace Microsoft.Ccr.Core
{
    internal class TaskExecutionWorker
    {
        public TaskExecutionWorker(Dispatcher Parent) => _dispatcher = Parent;

        internal void Shutdown()
        {
            _thread = null;
            _signal.Set();
        }
        internal bool Signal()
        {
            var evt = Interlocked.Exchange(ref _proxySignal, null);
            if (evt != null)
            {
                evt.Set();
                return true;
            }
            return false;
        }
        private void WaitForTask(bool doTimedWait)
        {
            if (_dispatcher._pendingTaskCount > 0 && _dispatcher.CachedDispatcherQueueCount != _dispatcher.SuspendedQueueCount)
                return;

            var evt = Interlocked.Exchange(ref _proxySignal, _signal);
            if (evt != null)
            {
                if (doTimedWait)
                {
                    _signal.WaitOne(1, true);
                    return;
                }
                _signal.WaitOne();
            }
        }
        private void CheckStartupComplete()
        {
            int workerCount = Interlocked.Increment(ref _dispatcher.WorkerCount);
            if (workerCount == _dispatcher.TaskExecutionWorkers.Count)
                _dispatcher.StartupCompleteEvent.Set();
        }
        private void CheckShutdownComplete()
        {
            if (Interlocked.Decrement(ref _dispatcher.WorkerCount) == 0)
                lock (_dispatcher.TaskExecutionWorkers)
                    _dispatcher.TaskExecutionWorkers.Clear();
        }
        public static void ExecuteInCurrentThreadContext(object t)
        {
            var task = (ITask)t;
            try
            {
                ExecuteTask(ref task, task.TaskQueue, false);
                Dispatcher.SetCurrentThreadCausalities(null);
            }
            catch (Exception ex)
            {
                if (IsCriticalException(ex)) throw;
                HandleException(task, ex);
            }
        }
        internal void ExecutionLoop()
        {
            CheckStartupComplete();
            while (true)
            {
                int dispatcherQueueCount = 0;
                int index = 0;
                ITask currentTask = null;
                try
                {
                    bool doTimedWait = false;
                    while (true)
                    {
                        if (dispatcherQueueCount == 0)
                        {
                            if (_thread == null) break;
                            WaitForTask(doTimedWait);
                        }
                        index++;
                        dispatcherQueueCount = 0;
                        var cachedDispatcherQueueCount = _dispatcher.CachedDispatcherQueueCount;
                        for (var i = 0; i < cachedDispatcherQueueCount; i++)
                        {
                            if (cachedDispatcherQueueCount != _dispatcher.CachedDispatcherQueueCount)
                                continue;
                            DispatcherQueue dispatcherQueue;
                            try
                            {
                                dispatcherQueue = _dispatcher._dispatcherQueues[(i + index) % cachedDispatcherQueueCount];
                            }
                            catch
                            {
                                continue;
                            }
                            doTimedWait |= dispatcherQueue.CheckTimerExpirations();
                            if (dispatcherQueue.TryDequeue(out currentTask))
                            {
                                dispatcherQueueCount += dispatcherQueue.Count;
                                ExecuteTask(ref currentTask, dispatcherQueue, false);
                            }
                        }
                    }
                    Dispatcher.ClearCausalities();
                    CheckShutdownComplete();
                    break;
                }
                catch (Exception ex)
                {
                    if (IsCriticalException(ex)) throw;
                    HandleException(currentTask, ex);
                    continue;
                }
            }
        }
        private static bool IsCriticalException(Exception exception) => exception is ExecutionEngineException || exception is OutOfMemoryException || exception is SEHException;
        private static void HandleException(ITask currentTask, Exception e)
        {
            Dispatcher.LogError(Resource.HandleExceptionLog, e);
            Dispatcher.FilterExceptionThroughCausalities(currentTask, e);
            Dispatcher.SetCurrentThreadCausalities(null);

            if (currentTask != null && currentTask.ArbiterCleanupHandler != null)
            {
                try
                {
                    currentTask.ArbiterCleanupHandler();
                }
                catch (Exception exception)
                {
                    Dispatcher.LogError(Resource.ExceptionDuringArbiterCleanup, exception);
                }
            }
        }
        private static void ExecuteTask(ref ITask currentTask, DispatcherQueue p, bool bypassExecute)
        {
            var cleanupHandler = currentTask.ArbiterCleanupHandler;
            IteratorContext iteratorContext;
            if (bypassExecute) 
                iteratorContext = null;
            else 
                iteratorContext = ExecuteTaskHelper(currentTask);
            if (iteratorContext == null) 
                iteratorContext = (IteratorContext)currentTask.LinkedIterator;

            if (iteratorContext != null)
            {
                if (iteratorContext.Causalities != null) 
                    Dispatcher.SetCurrentThreadCausalities(iteratorContext.Causalities);

                MoveIterator(ref currentTask, iteratorContext, ref cleanupHandler);

                if (currentTask != null)
                {
                    currentTask.LinkedIterator = iteratorContext;
                    currentTask.TaskQueue = p;
                    iteratorContext = ExecuteTaskHelper(currentTask);
                    if (iteratorContext != null) NestIterator(currentTask);
                }
            }
            if (cleanupHandler != null)
            {
                if (currentTask != null) 
                    currentTask.ArbiterCleanupHandler = null;
                cleanupHandler();
            }
        }
        private static void MoveIterator(ref ITask currentTask, IteratorContext iteratorContext, ref Handler finalizer)
        {
            lock (iteratorContext)
            {
                bool movedIterator = false;
                try
                {
                    movedIterator = !iteratorContext.Iterator.MoveNext();
                    if (!movedIterator)
                    {
                        currentTask = iteratorContext.Iterator.Current;
                        currentTask.ArbiterCleanupHandler = finalizer;
                        finalizer = null;
                    }
                    else
                    {
                        if (currentTask != null) finalizer = currentTask.ArbiterCleanupHandler;
                        else finalizer = null;
                        currentTask = null;
                    }
                }
                catch (Exception)
                {
                    iteratorContext.Iterator.Dispose();
                    throw;
                }
                finally
                {
                    if (movedIterator) 
                        iteratorContext.Iterator.Dispose();
                }
            }
        }
        private static void NestIterator(ITask currentTask)
        {
            var t = currentTask;
            var arbiterCleanup = t.ArbiterCleanupHandler;
            t.ArbiterCleanupHandler = () =>
            {
                t.ArbiterCleanupHandler = arbiterCleanup;
                ExecuteTask(ref t, t.TaskQueue, true);
            };

            currentTask.TaskQueue.Enqueue(t);
        }
        private static IteratorContext ExecuteTaskHelper(ITask currentTask)
        {
            if (currentTask.LinkedIterator != null)
            {
                var ctx = (IteratorContext)currentTask.LinkedIterator;
                if (ctx.Causalities != null) 
                    Dispatcher.SetCurrentThreadCausalities(ctx.Causalities);
            }
            else 
                Dispatcher.TransferCausalitiesFromTaskToCurrentThread(currentTask);

            var tasks = currentTask.Execute();
            if (tasks != null) 
                return new IteratorContext(tasks, Dispatcher.GetCurrentThreadCausalities());

            return null;
        }

        private readonly Dispatcher _dispatcher;
        internal Thread _thread;
        private readonly AutoResetEvent _signal = new(false);
        private AutoResetEvent _proxySignal;
    }
}
