using System;
using Microsoft.Ccr.Core;

namespace MFDLabs.Concurrency
{
    /// <summary>
    /// Represents a CCR based work queue.
    /// </summary>
    public class AsyncWorkQueue<T>
    {
        internal class WorkItem
        {
            private readonly Action _CompletionTask;
            private readonly T _Item;
            private readonly SuccessFailurePort _Result;

            internal Action CompletionTask
            {
                get { return _CompletionTask; }
            }
            internal T Item
            {
                get { return _Item; }
            }
            internal SuccessFailurePort Result
            {
                get { return _Result; }
            }

            internal WorkItem(T item)
            {
                _Item = item;
            }
            internal WorkItem(T item, Action completionTask)
            {
                _CompletionTask = completionTask;
                _Item = item;
            }
            internal WorkItem(T item, SuccessFailurePort result)
            {
                _Item = item;
                _Result = result;
            }
        }

        private readonly DispatcherQueue _DispatcherQueue;
        private readonly AsyncItemHandler _ItemHandler;
        private readonly Port<WorkItem> _QueuedItems = new Port<WorkItem>();

        /// <summary>
        /// Constructs a new CCR based WorkQueue
        /// </summary>
        /// <param name="dispatcherQueue"></param>
        /// <param name="itemHandler"></param>
        public AsyncWorkQueue(DispatcherQueue dispatcherQueue, AsyncItemHandler itemHandler)
        {
            _DispatcherQueue = dispatcherQueue;
            _ItemHandler = itemHandler ?? throw new ApplicationException("AsyncWorkQueue initialization failed. Valid AsyncItemHandler required.");

            var receiver = Arbiter.Receive<WorkItem>(
                true,
                _QueuedItems,
                (workItem) => DoWork(workItem)
            );
            Arbiter.Activate(_DispatcherQueue, receiver);
        }

        private void DoCompletionTask(SuccessFailurePort itemHandlerResult, Action completionTask)
        {
            var choice = Arbiter.Choice(
                itemHandlerResult,
                (success) => completionTask(),
                (failure) => ExceptionHandler.LogException(failure)
            );
            Arbiter.Activate(_DispatcherQueue, choice);
        }
        private void DoWork(WorkItem workItem)
        {
            SuccessFailurePort result;

            if (workItem.Result != null)
                result = workItem.Result;
            else
                result = new SuccessFailurePort();

            _ItemHandler(workItem.Item, result);

            if (workItem.CompletionTask != null)
                DoCompletionTask(result, workItem.CompletionTask);
        }

        /// <summary>
        /// Queue a work item
        /// </summary>
        /// <param name="item"></param>
        public void EnqueueWorkItem(T item)
        {
            _QueuedItems.Post(new WorkItem(item));
        }
        /// <summary>
        /// Queue a work item
        /// </summary>
        /// <param name="item"></param>
        /// <param name="completionTask"></param>
        public void EnqueueWorkItem(T item, Action completionTask)
        {
            _QueuedItems.Post(new WorkItem(item, completionTask));
        }
        /// <summary>
        /// Queue a work item
        /// </summary>
        /// <param name="item"></param>
        /// <param name="result"></param>
        public void EnqueueWorkItem(T item, SuccessFailurePort result)
        {
            _QueuedItems.Post(new WorkItem(item, result));
        }

        /// <summary>
        /// Handler
        /// </summary>
        /// <param name="item"></param>
        /// <param name="result"></param>
        public delegate void AsyncItemHandler(T item, SuccessFailurePort result);
    }
}
