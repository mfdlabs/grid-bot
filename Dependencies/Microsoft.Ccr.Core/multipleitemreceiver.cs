using Microsoft.Ccr.Core.Arbiters;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Ccr.Core
{
    public class MultipleItemReceiver : ReceiverTask
    {
        public MultipleItemReceiver(ITask userTask, params IPortReceive[] ports)
        {
            if (ports == null)
            {
                throw new ArgumentNullException("ports");
            }
            if (userTask == null)
            {
                throw new ArgumentNullException("userTask");
            }
            if (ports.Length == 0)
            {
                throw new ArgumentOutOfRangeException("ports");
            }
            this._ports = ports;
            this._userTask = userTask;
            this._pendingItemCount = ports.Length;
            this._receivers = new Receiver[this._ports.Length];
        }

        public new ITask PartialClone()
        {
            return new MultipleItemReceiver(this._userTask.PartialClone(), this._ports);
        }

        public override IEnumerator<ITask> Execute()
        {
            base.Execute();
            return null;
        }

        private void Register()
        {
            int num = 0;
            foreach (IPortReceive port in this._ports)
            {
                Receiver receiver = new MultipleItemHelperReceiver(port, this);
                receiver._arbiterContext = num;
                this._receivers[num++] = receiver;
                receiver.TaskQueue = base.TaskQueue;
            }
            num = 0;
            foreach (IPortReceive portReceive in this._ports)
            {
                portReceive.RegisterReceiver(this._receivers[num++]);
            }
        }

        internal bool Evaluate(int index, IPortElement item, ref ITask deferredTask)
        {
            if (base.State == ReceiverTaskState.CleanedUp)
            {
                return false;
            }
            if (this._userTask[index] != null)
            {
                throw new InvalidOperationException();
            }
            this._userTask[index] = item;
            int num = Interlocked.Decrement(ref this._pendingItemCount);
            if (num > 0)
            {
                return true;
            }
            if (num == 0)
            {
                this._userTask.LinkedIterator = base.LinkedIterator;
                this._userTask.TaskQueue = base.TaskQueue;
                this._userTask.ArbiterCleanupHandler = base.ArbiterCleanupHandler;
                deferredTask = this._userTask;
                if (this.Arbiter != null)
                {
                    if (!this.Arbiter.Evaluate(this, ref deferredTask))
                    {
                        return false;
                    }
                    this._userTask = null;
                }
                return true;
            }
            return false;
        }

        public override IArbiterTask Arbiter
        {
            set
            {
                base.Arbiter = value;
                if (base.TaskQueue == null)
                {
                    base.TaskQueue = base.Arbiter.TaskQueue;
                }
                this.Register();
            }
        }

        public override bool Evaluate(IPortElement messageNode, ref ITask deferredTask)
        {
            throw new NotImplementedException();
        }

        public override void Consume(IPortElement item)
        {
            throw new NotImplementedException();
        }

        public override void Cleanup()
        {
            base.State = ReceiverTaskState.CleanedUp;
            foreach (Receiver receiver in this._receivers)
            {
                if (receiver != null)
                {
                    receiver._port.UnregisterReceiver(receiver);
                }
            }
            if (this._userTask != null)
            {
                this.Cleanup(this._userTask);
            }
        }

        public override void Cleanup(ITask taskToCleanup)
        {
            for (int i = 0; i < this._ports.Length; i++)
            {
                IPortElement portElement = taskToCleanup[i];
                if (portElement != null)
                {
                    ((IPort)this._ports[i]).TryPostUnknownType(taskToCleanup[i].Item);
                }
            }
        }

        private ITask _userTask;

        private IPortReceive[] _ports;

        private Receiver[] _receivers;

        private int _pendingItemCount;
    }
}
