using System.Collections.Generic;
using Microsoft.Ccr.Core.Arbiters;

namespace Microsoft.Ccr.Core
{
    public abstract class TaskCommon : ITask
    {
        public Handler ArbiterCleanupHandler
        {
            get => _ArbiterCleanupHandler;
            set => _ArbiterCleanupHandler = value;
        }
        public object LinkedIterator
        {
            get => _linkedIterator;
            set => _linkedIterator = value;
        }
        public DispatcherQueue TaskQueue
        {
            get => _dispatcherQueue;
            set => _dispatcherQueue = value;
        }
        public abstract IPortElement this[int index] { get; set; }
        public abstract int PortElementCount { get; }

        public abstract ITask PartialClone();
        public abstract IEnumerator<ITask> Execute();

        internal TaskCommon _previous;
        internal TaskCommon _next;
        private Handler _ArbiterCleanupHandler;
        private object _linkedIterator;
        private DispatcherQueue _dispatcherQueue;
    }
}
