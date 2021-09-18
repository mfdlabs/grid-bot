using Microsoft.Ccr.Core.Properties;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Ccr.Core
{
    internal class TaskExecutionWorker
    {
        public TaskExecutionWorker(Dispatcher Parent)
        {
            this._dispatcher = Parent;
        }

        internal void Shutdown()
        {
            this._thread = null;
            this._signal.Set();
        }

        internal bool Signal()
        {
            AutoResetEvent autoResetEvent = Interlocked.Exchange<AutoResetEvent>(ref this._proxySignal, null);
            if (autoResetEvent != null)
            {
                autoResetEvent.Set();
                return true;
            }
            return false;
        }

        private void WaitForTask(bool doTimedWait)
        {
            if (this._dispatcher._pendingTaskCount > 0 && this._dispatcher._cachedDispatcherQueueCount != this._dispatcher._suspendedQueueCount)
            {
                return;
            }
            AutoResetEvent autoResetEvent = Interlocked.Exchange<AutoResetEvent>(ref this._proxySignal, this._signal);
            if (autoResetEvent != null)
            {
                if (doTimedWait)
                {
                    this._signal.WaitOne(1, true);
                    return;
                }
                this._signal.WaitOne();
            }
        }

        private void CheckStartupComplete()
        {
            int num = Interlocked.Increment(ref this._dispatcher._workerCount);
            if (num == this._dispatcher._taskExecutionWorkers.Count)
            {
                this._dispatcher._startupCompleteEvent.Set();
            }
        }

        private void CheckShutdownComplete()
        {
            if (Interlocked.Decrement(ref this._dispatcher._workerCount) == 0)
            {
                lock (this._dispatcher._taskExecutionWorkers)
                {
                    this._dispatcher._taskExecutionWorkers.Clear();
                }
            }
        }

        public static void ExecuteInCurrentThreadContext(object t)
        {
            ITask task = (ITask)t;
            try
            {
                ExecuteTask(ref task, task.TaskQueue, false);
                Dispatcher.SetCurrentThreadCausalities(null);
            }
            catch (Exception ex)
            {
                if (IsCriticalException(ex))
                {
                    throw;
                }
                HandleException(task, ex);
            }
        }

        internal void ExecutionLoop()
        {
            this.CheckStartupComplete();
            for (; ; )
            {
                LOOP_JMP_BACK:
                int num = 0;
                int num2 = 0;
                ITask currentTask = null;
                try
                {
                    bool flag = false;
                    for (; ; )
                    {
                        if (num == 0)
                        {
                            if (this._thread == null)
                            {
                                break;
                            }
                            this.WaitForTask(flag);
                        }
                        num2++;
                        num = 0;
                        int cachedDispatcherQueueCount = this._dispatcher._cachedDispatcherQueueCount;
                        for (int i = 0; i < cachedDispatcherQueueCount; i++)
                        {
                            if (cachedDispatcherQueueCount != this._dispatcher._cachedDispatcherQueueCount)
                            {
                                goto CONTINUE_EXECUTING;
                            }
                            DispatcherQueue dispatcherQueue;
                            try
                            {
                                dispatcherQueue = this._dispatcher._dispatcherQueues[(i + num2) % cachedDispatcherQueueCount];
                            }
                            catch
                            {
                                goto LOOP_JMP_BACK;
                            }
                            flag |= dispatcherQueue.CheckTimerExpirations();
                            if (dispatcherQueue.TryDequeue(out currentTask))
                            {
                                num += dispatcherQueue.Count;
                                ExecuteTask(ref currentTask, dispatcherQueue, false);
                            }
                        }
                    }
                    Dispatcher.ClearCausalities();
                    this.CheckShutdownComplete();
                    break;
                    CONTINUE_EXECUTING:
                    continue;
                }
                catch (Exception ex)
                {
                    if (IsCriticalException(ex))
                    {
                        throw;
                    }
                    HandleException(currentTask, ex);
                    continue;
                }
            }
        }

        private static bool IsCriticalException(Exception exception)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return exception is ExecutionEngineException || exception is OutOfMemoryException || exception is SEHException;
#pragma warning restore CS0618 // Type or member is obsolete
        }

        private static void HandleException(ITask currentTask, Exception e)
        {
            Dispatcher.LogError(Resource1.HandleExceptionLog, e);
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
                    Dispatcher.LogError(Resource1.ExceptionDuringArbiterCleanup, exception);
                }
            }
        }

        private static void ExecuteTask(ref ITask currentTask, DispatcherQueue p, bool bypassExecute)
        {
            Handler arbiterCleanupHandler = currentTask.ArbiterCleanupHandler;
            IteratorContext iteratorContext;
            if (bypassExecute)
            {
                iteratorContext = null;
            }
            else
            {
                iteratorContext = ExecuteTaskHelper(currentTask);
            }
            if (iteratorContext == null)
            {
                iteratorContext = (IteratorContext)currentTask.LinkedIterator;
            }
            if (iteratorContext != null)
            {
                if (iteratorContext._causalities != null)
                {
                    Dispatcher.SetCurrentThreadCausalities(iteratorContext._causalities);
                }
                MoveIterator(ref currentTask, iteratorContext, ref arbiterCleanupHandler);
                if (currentTask != null)
                {
                    currentTask.LinkedIterator = iteratorContext;
                    currentTask.TaskQueue = p;
                    iteratorContext = ExecuteTaskHelper(currentTask);
                    if (iteratorContext != null)
                    {
                        NestIterator(currentTask);
                    }
                }
            }
            if (arbiterCleanupHandler != null)
            {
                if (currentTask != null)
                {
                    currentTask.ArbiterCleanupHandler = null;
                }
                arbiterCleanupHandler();
            }
        }

        private static void MoveIterator(ref ITask currentTask, IteratorContext iteratorContext, ref Handler finalizer)
        {
            lock (iteratorContext)
            {
                bool flag = false;
                try
                {
                    flag = !iteratorContext._iterator.MoveNext();
                    if (!flag)
                    {
                        currentTask = iteratorContext._iterator.Current;
                        currentTask.ArbiterCleanupHandler = finalizer;
                        finalizer = null;
                    }
                    else
                    {
                        if (currentTask != null)
                        {
                            finalizer = currentTask.ArbiterCleanupHandler;
                        }
                        else
                        {
                            finalizer = null;
                        }
                        currentTask = null;
                    }
                }
                catch (Exception)
                {
                    iteratorContext._iterator.Dispose();
                    throw;
                }
                finally
                {
                    if (flag)
                    {
                        iteratorContext._iterator.Dispose();
                    }
                }
            }
        }

        private static void NestIterator(ITask currentTask)
        {
            ITask t = currentTask;
            Handler arbiterCleanup = t.ArbiterCleanupHandler;
            t.ArbiterCleanupHandler = delegate ()
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
                IteratorContext iteratorContext = (IteratorContext)currentTask.LinkedIterator;
                if (iteratorContext._causalities != null)
                {
                    Dispatcher.SetCurrentThreadCausalities(iteratorContext._causalities);
                }
            }
            else
            {
                Dispatcher.TransferCausalitiesFromTaskToCurrentThread(currentTask);
            }
            IEnumerator<ITask> enumerator = currentTask.Execute();
            if (enumerator != null)
            {
                return new IteratorContext(enumerator, Dispatcher.GetCurrentThreadCausalities());
            }
            return null;
        }

        private readonly Dispatcher _dispatcher;

        internal Thread _thread;

        private readonly AutoResetEvent _signal = new AutoResetEvent(false);

        private AutoResetEvent _proxySignal;
    }
}
