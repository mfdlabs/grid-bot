using Microsoft.Ccr.Core.Arbiters;
using System.Threading;

namespace Microsoft.Ccr.Core
{
    public abstract class JoinReceiverTask : ReceiverTask, IArbiterTask, ITask
    {
        internal JoinReceiverTask()
        {
        }

        internal JoinReceiverTask(ITask UserTask) : base(UserTask)
        {
        }

        public override IArbiterTask Arbiter
        {
            get
            {
                return base.Arbiter;
            }
            set
            {
                base.Arbiter = value;
                if (base.TaskQueue == null)
                {
                    base.TaskQueue = base.Arbiter.TaskQueue;
                }
                this.Commit();
                if (this._state == ReceiverTaskState.CleanedUp)
                {
                    return;
                }
                this.Register();
            }
        }

        public ArbiterTaskState ArbiterState
        {
            get
            {
                if (this._arbiter != null)
                {
                    return this._arbiter.ArbiterState;
                }
                if (base.State == ReceiverTaskState.CleanedUp)
                {
                    return ArbiterTaskState.Done;
                }
                return ArbiterTaskState.Active;
            }
        }

        protected abstract void Register();

        public abstract override void Cleanup(ITask taskToCleanup);

        protected abstract bool ShouldCommit();

        public bool Evaluate(ReceiverTask receiver, ref ITask deferredTask)
        {
            deferredTask = null;
            if (this.ShouldCommit())
            {
                deferredTask = new Task(new Handler(this.Commit));
            }
            return false;
        }

        protected abstract void Commit();

        protected void Arbitrate(ITask winner, IPortElement[] items, bool allTaken)
        {
            if (allTaken)
            {
                if (this._state == ReceiverTaskState.Onetime && this._arbiter == null)
                {
                    int num = Interlocked.Increment(ref this._commitAttempt);
                    if (num > 1)
                    {
                        return;
                    }
                }
                ITask task = winner;
                if (this._arbiter == null || this._arbiter.Evaluate(this, ref task))
                {
                    if (this._arbiter == null && task != null)
                    {
                        task.LinkedIterator = base.LinkedIterator;
                        task.ArbiterCleanupHandler = base.ArbiterCleanupHandler;
                    }
                    if (task != null)
                    {
                        base.TaskQueue.Enqueue(task);
                    }
                    if (this._state == ReceiverTaskState.Onetime)
                    {
                        this.Cleanup();
                    }
                    return;
                }
            }
            base.TaskQueue.Enqueue(new Task<IPortElement[]>(items, new Handler<IPortElement[]>(this.UnrollPartialCommit)));
        }

        protected abstract void UnrollPartialCommit(IPortElement[] items);

        private int _commitAttempt;
    }
}
