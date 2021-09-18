using Microsoft.Ccr.Core.Arbiters;
using Microsoft.Ccr.Core.Properties;
using System;

namespace Microsoft.Ccr.Core
{
    public class JoinSinglePortReceiver : JoinReceiverTask
    {
        internal JoinSinglePortReceiver()
        {
        }

        public JoinSinglePortReceiver(bool persist, ITask task, IPortReceive port, int count) : base(task)
        {
            if (persist)
            {
                this._state = ReceiverTaskState.Persistent;
            }
            if (count <= 0)
            {
                throw new ArgumentException(Resource1.JoinSinglePortReceiverAtLeastOneItemMessage, "count");
            }
            this._port = port;
            this._count = count;
        }

        public override void Cleanup()
        {
            base.Cleanup();
            this._port.UnregisterReceiver(this);
        }

        public override void Cleanup(ITask taskToCleanup)
        {
            if (taskToCleanup == null)
            {
                throw new ArgumentNullException("taskToCleanup");
            }
            for (int i = 0; i < this._count; i++)
            {
                ((IPortArbiterAccess)this._port).PostElement(taskToCleanup[i]);
            }
        }

        protected override void Register()
        {
            this._port.RegisterReceiver(this);
        }

        protected override bool ShouldCommit()
        {
            if (this._state != ReceiverTaskState.CleanedUp)
            {
                if (this._arbiter != null && this._arbiter.ArbiterState != ArbiterTaskState.Active)
                {
                    return false;
                }
                if (this._port.ItemCount >= this._count)
                {
                    return true;
                }
            }
            return false;
        }

        public override bool Evaluate(IPortElement messageNode, ref ITask deferredTask)
        {
            deferredTask = null;
            if (this.ShouldCommit())
            {
                deferredTask = new Task(new Handler(this.Commit));
            }
            return false;
        }

        public override void Consume(IPortElement item)
        {
            if (this.ShouldCommit())
            {
                base.TaskQueue.Enqueue(new Task(new Handler(this.Commit)));
            }
        }

        protected override void Commit()
        {
            ITask task = base.UserTask.PartialClone();
            IPortElement[] array = ((IPortArbiterAccess)this._port).TestForMultipleElements(this._count);
            if (array != null)
            {
                for (int i = 0; i < this._count; i++)
                {
                    task[i] = array[i];
                }
                base.Arbitrate(task, array, true);
            }
        }

        protected override void UnrollPartialCommit(IPortElement[] items)
        {
            IPortArbiterAccess portArbiterAccess = (IPortArbiterAccess)this._port;
            for (int i = 0; i < this._count; i++)
            {
                if (items[i] != null)
                {
                    portArbiterAccess.PostElement(items[i]);
                }
            }
        }

        private IPortReceive _port;

        private int _count;
    }
}
