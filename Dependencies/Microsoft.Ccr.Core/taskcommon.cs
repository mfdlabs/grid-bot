using Microsoft.Ccr.Core.Arbiters;
using System.Collections.Generic;

namespace Microsoft.Ccr.Core
{
    public abstract class TaskCommon : ITask
    {
        public Handler ArbiterCleanupHandler
        {
            get
            {
                return this._ArbiterCleanupHandler;
            }
            set
            {
                this._ArbiterCleanupHandler = value;
            }
        }

        public object LinkedIterator
        {
            get
            {
                return this._linkedIterator;
            }
            set
            {
                this._linkedIterator = value;
            }
        }

        public DispatcherQueue TaskQueue
        {
            get
            {
                return this._dispatcherQueue;
            }
            set
            {
                this._dispatcherQueue = value;
            }
        }

        public abstract ITask PartialClone();

        public abstract IPortElement this[int index]
        {
            get;
            set;
        }

        public abstract int PortElementCount { get; }

        public abstract IEnumerator<ITask> Execute();

        internal TaskCommon _previous;

        internal TaskCommon _next;

        private Handler _ArbiterCleanupHandler;

        private object _linkedIterator;

        private DispatcherQueue _dispatcherQueue;
    }
}
