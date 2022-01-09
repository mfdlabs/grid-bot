using System.Threading;
using Microsoft.Ccr.Core.Arbiters;

namespace Microsoft.Ccr.Core
{
    public abstract class JoinReceiverTask : ReceiverTask, IArbiterTask
    {
        internal JoinReceiverTask()
        {}
        internal JoinReceiverTask(ITask userTask) : base(userTask)
        {}

        public override IArbiterTask Arbiter
        {
            get => base.Arbiter;
            set
            {
                base.Arbiter = value;
                TaskQueue ??= base.Arbiter.TaskQueue;
                Commit();
                if (_state == ReceiverTaskState.CleanedUp) return;
                Register();
            }
        }
        public ArbiterTaskState ArbiterState
        {
            get
            {
                if (_arbiter != null) return _arbiter.ArbiterState;
                return State == ReceiverTaskState.CleanedUp ? ArbiterTaskState.Done : ArbiterTaskState.Active;
            }
        }

        protected abstract void Register();
        public abstract override void Cleanup(ITask taskToCleanup);
        protected abstract bool ShouldCommit();
        public bool Evaluate(ReceiverTask receiver, ref ITask deferredTask)
        {
            deferredTask = null;
            if (ShouldCommit()) deferredTask = new Task(Commit);
            return false;
        }
        protected abstract void Commit();
        protected void Arbitrate(ITask winner, IPortElement[] items, bool allTaken)
        {
            if (allTaken)
            {
                if (_state == ReceiverTaskState.Onetime && _arbiter == null)
                {
                    var attempt = Interlocked.Increment(ref _commitAttempt);
                    if (attempt > 1) return;
                }
                var task = winner;
                if (_arbiter == null || _arbiter.Evaluate(this, ref task))
                {
                    if (_arbiter == null && task != null)
                    {
                        task.LinkedIterator = LinkedIterator;
                        task.ArbiterCleanupHandler = ArbiterCleanupHandler;
                    }
                    if (task != null) TaskQueue.Enqueue(task);
                    if (_state == ReceiverTaskState.Onetime) Cleanup();
                    return;
                }
            }
            TaskQueue.Enqueue(new Task<IPortElement[]>(items, UnrollPartialCommit));
        }
        protected abstract void UnrollPartialCommit(IPortElement[] items);

        private int _commitAttempt;
    }
}
