using Microsoft.Ccr.Core.Arbiters;
using Microsoft.Ccr.Core.Properties;
using System;
using System.Globalization;
using System.Text;

namespace Microsoft.Ccr.Core
{
    public class JoinReceiver : JoinReceiverTask
    {
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (Receiver receiver in this._ports)
            {
                stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "[{0}({1})] ", new object[]
                {
                    receiver._port.GetType().ToString(),
                    receiver._port.ItemCount.ToString(CultureInfo.InvariantCulture)
                });
            }
            return string.Format(CultureInfo.InvariantCulture, "\t{0}({1}) waiting on ports {2} with {3} nested under \n    {4}", new object[]
            {
                base.GetType().Name,
                this._state,
                stringBuilder.ToString(),
                (base.UserTask == null) ? "no continuation" : ("method " + base.UserTask.ToString()),
                (this._arbiter == null) ? "none" : this._arbiter.ToString()
            });
        }

        internal JoinReceiver()
        {
        }

        public JoinReceiver(bool persist, ITask task, params IPortReceive[] ports) : base(task)
        {
            if (ports == null)
            {
                throw new ArgumentNullException("ports");
            }
            if (persist)
            {
                this._state = ReceiverTaskState.Persistent;
            }
            if (ports == null || ports.Length == 0)
            {
                throw new ArgumentOutOfRangeException("aP", Resource1.JoinsMustHaveOnePortMinimumException);
            }
            this._ports = new Receiver[ports.Length];
            int[] array = new int[ports.Length];
            int num = 0;
            foreach (IPortReceive portReceive in ports)
            {
                int hashCode = portReceive.GetHashCode();
                Receiver receiver = new Receiver(portReceive);
                this._ports[num] = receiver;
                array[num] = hashCode;
                receiver.ArbiterContext = num;
                num++;
            }
            Array.Sort<int, Receiver>(array, this._ports);
        }

        public override void Cleanup()
        {
            base.Cleanup();
            foreach (Receiver receiver in this._ports)
            {
                receiver.Cleanup();
            }
        }

        public override void Cleanup(ITask taskToCleanup)
        {
            if (taskToCleanup == null)
            {
                throw new ArgumentNullException("taskToCleanup");
            }
            for (int i = 0; i < this._ports.Length; i++)
            {
                Receiver receiver = this._ports[i];
                ((IPortArbiterAccess)receiver._port).PostElement(taskToCleanup[(int)receiver.ArbiterContext]);
            }
        }

        protected override void Register()
        {
            foreach (Receiver receiver in this._ports)
            {
                if (this._state == ReceiverTaskState.Persistent)
                {
                    receiver._state = ReceiverTaskState.Persistent;
                }
                receiver.Arbiter = this;
            }
        }

        public override bool Evaluate(IPortElement messageNode, ref ITask deferredTask)
        {
            deferredTask = null;
            return false;
        }

        public override void Consume(IPortElement item)
        {
        }

        protected override bool ShouldCommit()
        {
            if (this._state == ReceiverTaskState.CleanedUp)
            {
                return false;
            }
            if (this._arbiter == null || this._arbiter.ArbiterState == ArbiterTaskState.Active)
            {
                foreach (Receiver receiver in this._ports)
                {
                    if (receiver._port.ItemCount == 0)
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        protected override void Commit()
        {
            if (!this.ShouldCommit())
            {
                return;
            }
            ITask task = base.UserTask.PartialClone();
            IPortElement[] array = new IPortElement[this._ports.Length];
            bool allTaken = true;
            for (int i = 0; i < this._ports.Length; i++)
            {
                Receiver receiver = this._ports[i];
                IPortElement portElement = ((IPortArbiterAccess)receiver._port).TestForElement();
                if (portElement == null)
                {
                    allTaken = false;
                    break;
                }
                array[i] = portElement;
                task[(int)receiver.ArbiterContext] = portElement;
            }
            base.Arbitrate(task, array, allTaken);
        }

        protected override void UnrollPartialCommit(IPortElement[] items)
        {
            for (int i = 0; i < this._ports.Length; i++)
            {
                if (items[i] != null)
                {
                    ((IPortArbiterAccess)items[i].Owner).PostElement(items[i]);
                }
            }
        }

        private Receiver[] _ports;
    }
}
