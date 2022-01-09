using System;
using MFDLabs.Logging;
using Microsoft.Ccr.Core;

// ReSharper disable once CheckNamespace
namespace MFDLabs.Concurrency
{
    /// <summary>
    /// Represents a CCR based work queue.
    /// </summary>
    public class AsyncWorkQueue<T>
    {
        private class WorkItem
        {
            private readonly Action _completionTask;
            private readonly T _item;
            private readonly SuccessFailurePort _result;

            internal Action CompletionTask => _completionTask;

            internal T Item => _item;

            internal SuccessFailurePort Result => _result;

            internal WorkItem(T item)
            {
                _item = item;
            }
            internal WorkItem(T item, Action completionTask)
            {
                _completionTask = completionTask;
                _item = item;
            }
            internal WorkItem(T item, SuccessFailurePort result)
            {
                _item = item;
                _result = result;
            }
        }

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly AsyncItemHandler _itemHandler;
        private readonly Port<WorkItem> _queuedItems = new Port<WorkItem>();

        /// <summary>
        /// Constructs a new CCR based WorkQueue
        /// </summary>
        /// <param name="dispatcherQueue"></param>
        /// <param name="itemHandler"></param>
        public AsyncWorkQueue(DispatcherQueue dispatcherQueue, AsyncItemHandler itemHandler)
        {
            _dispatcherQueue = dispatcherQueue;
            _itemHandler = itemHandler ?? throw new ApplicationException("AsyncWorkQueue initialization failed. Valid AsyncItemHandler required.");

            var receiver = Arbiter.Receive(
                true,
                _queuedItems,
                DoWork
            );
            Arbiter.Activate(_dispatcherQueue, receiver);
        }

        private void DoCompletionTask(SuccessFailurePort itemHandlerResult, Action completionTask)
        {
            var choice = Arbiter.Choice(
                itemHandlerResult,
                (success) => completionTask(),
                SystemLogger.Singleton.Error
            );
            Arbiter.Activate(_dispatcherQueue, choice);
        }
        private void DoWork(WorkItem workItem)
        {
            var result = workItem.Result ?? new SuccessFailurePort();

            _itemHandler(workItem.Item, result);

            if (workItem.CompletionTask != null)
                DoCompletionTask(result, workItem.CompletionTask);
        }

        /// <summary>
        /// Queue a work item
        /// </summary>
        /// <param name="item"></param>
        public void EnqueueWorkItem(T item)
        {
            _queuedItems.Post(new WorkItem(item));
        }
        /// <summary>
        /// Queue a work item
        /// </summary>
        /// <param name="item"></param>
        /// <param name="completionTask"></param>
        public void EnqueueWorkItem(T item, Action completionTask)
        {
            _queuedItems.Post(new WorkItem(item, completionTask));
        }
        /// <summary>
        /// Queue a work item
        /// </summary>
        /// <param name="item"></param>
        /// <param name="result"></param>
        public void EnqueueWorkItem(T item, SuccessFailurePort result)
        {
            _queuedItems.Post(new WorkItem(item, result));
        }

        /// <summary>
        /// Handler
        /// </summary>
        /// <param name="item"></param>
        /// <param name="result"></param>
        public delegate void AsyncItemHandler(T item, SuccessFailurePort result);
    }
}
