using Microsoft.Ccr.Core;

namespace MFDLabs.Concurrency
{
    /// <inheritdoc/>
    public class PatchedDispatcherQueue : DispatcherQueue
    {
        /// <inheritdoc/>
        public PatchedDispatcherQueue()
            : base()
        {
        }

        /// <inheritdoc/>
        public PatchedDispatcherQueue(string name)
            : base(name)
        {
        }

        /// <inheritdoc/>
        public PatchedDispatcherQueue(string name, Dispatcher dispatcher)
            : base(name, dispatcher)
        {
        }

        /// <inheritdoc/>
        public PatchedDispatcherQueue(string name, Dispatcher dispatcher, TaskExecutionPolicy policy, int maximumQueueDepth)
            : base(name, dispatcher, policy, maximumQueueDepth)
        {
        }

        /// <inheritdoc/>
        public PatchedDispatcherQueue(string name, Dispatcher dispatcher, TaskExecutionPolicy policy, double schedulingRate)
            : base(name, dispatcher, policy, schedulingRate)
        {
        }
    }
}
