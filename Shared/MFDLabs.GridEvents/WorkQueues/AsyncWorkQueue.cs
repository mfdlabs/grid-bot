using System;
using System.Threading;
using System.Collections.Concurrent;

namespace MFDLabs.Grid.Bot.WorkQueues
{
    public class AsyncWorkQueue<T>
    {
        internal class WorkItem
        {
            private readonly Action _CompletionTask;
            private readonly T _Item;

            internal Action CompletionTask
            {
                get { return _CompletionTask; }
            }
            internal T Item
            {
                get { return _Item; }
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
        }

        private readonly Thread _BackgroundThread;
        private readonly AsyncItemHandler _ItemHandler;
        private readonly SemaphoreSlim _Signal = new(0);
        private readonly CancellationToken _Cancel = new();
        private readonly ConcurrentQueue<WorkItem> _WorkItems = new();

        public AsyncWorkQueue(AsyncItemHandler itemHandler)
        {
            _ItemHandler = itemHandler ?? throw new ApplicationException("AsyncWorkQueue initialization failed.  Valid AsyncItemHandler required.");
            _BackgroundThread = new(BackgroundWork) { IsBackground = true };

            _BackgroundThread.Start();
        }

        private void DoWork(WorkItem workItem)
        {
            _ItemHandler(workItem.Item);

            workItem.CompletionTask?.Invoke();
        }

        private async void BackgroundWork(object _)
        {
            while (_Cancel.IsCancellationRequested == false)
            {
                await _Signal.WaitAsync(_Cancel);

                _WorkItems.TryDequeue(out var item);

                DoWork(item);
            }
        }

        public void EnqueueWorkItem(T item)
        {
            _WorkItems.Enqueue(new WorkItem(item));
            _Signal.Release();
        }

        public void EnqueueWorkItem(T item, Action completionTask)
        {
            _WorkItems.Enqueue(new WorkItem(item, completionTask));
            _Signal.Release();
        }

        public delegate void AsyncItemHandler(T item);
    }
}
