using Microsoft.Ccr.Core.Arbiters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Ccr.Core
{
    public class MultipleItemGather : ReceiverTask, ITask
    {
        public MultipleItemGather(Type[] types, IPortReceive[] ports, int itemCount, Handler<ICollection[]> handler)
        {
            this._state = ReceiverTaskState.Onetime;
            if (ports == null)
            {
                throw new ArgumentNullException("ports");
            }
            if (types == null)
            {
                throw new ArgumentNullException("types");
            }
            if (ports.Length == 0)
            {
                throw new ArgumentOutOfRangeException("ports");
            }
            if (types.Length == 0)
            {
                throw new ArgumentOutOfRangeException("types");
            }
            if (types.Length != ports.Length)
            {
                throw new ArgumentOutOfRangeException("types", "Type array length must match port array length");
            }
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }
            this._types = types;
            this._ports = ports;
            this._handler = handler;
            this._pendingItemCount = itemCount;
            this._expectedItemCount = itemCount;
            foreach (Type key in this._types)
            {
                this._lookupTable.Add(key, new List<object>());
            }
        }

        public new ITask PartialClone()
        {
            return new MultipleItemGather(this._types, this._ports, this._pendingItemCount, this._handler);
        }

        public override IEnumerator<ITask> Execute()
        {
            base.Execute();
            this.Register();
            return null;
        }

        private void Register()
        {
            int num = 0;
            this._receivers = new Receiver[this._ports.Length];
            foreach (IPortReceive portReceive in this._ports)
            {
                Receiver receiver = new GatherPrivateReceiver(portReceive, this);
                this._receivers[num++] = receiver;
                receiver.TaskQueue = base.TaskQueue;
                portReceive.RegisterReceiver(receiver);
                if (this._pendingItemCount <= 0)
                {
                    return;
                }
            }
        }

        internal bool Evaluate(object item, ref ITask deferredTask)
        {
            int num = Interlocked.Decrement(ref this._pendingItemCount);
            if (num < 0 || this._state == ReceiverTaskState.CleanedUp)
            {
                return false;
            }
            Type type = item.GetType();
            List<object> list;
            this._lookupTable.TryGetValue(type, out list);
            while (list == null)
            {
                type = type.BaseType;
                if (type == null)
                {
                    break;
                }
                this._lookupTable.TryGetValue(type, out list);
            }
            if (list == null)
            {
                throw new InvalidOperationException("No result collection found for type:" + type);
            }
            lock (list)
            {
                list.Add(item);
            }
            if (num != 0)
            {
                return true;
            }
            ICollection[] array = new ICollection[this._ports.Length];
            int num2 = 0;
            foreach (Type key in this._types)
            {
                array[num2++] = this._lookupTable[key];
            }
            deferredTask = new Task<ICollection[]>(array, this._handler)
            {
                LinkedIterator = base.LinkedIterator,
                TaskQueue = base.TaskQueue,
                ArbiterCleanupHandler = base.ArbiterCleanupHandler
            };
            if (this.Arbiter == null)
            {
                this._lookupTable.Clear();
                this.Cleanup();
                return true;
            }
            if (!this.Arbiter.Evaluate(this, ref deferredTask))
            {
                return false;
            }
            this._lookupTable.Clear();
            return true;
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

        public override void Cleanup()
        {
            base.Cleanup();
            foreach (Receiver receiver in this._receivers)
            {
                if (receiver != null)
                {
                    receiver._port.UnregisterReceiver(receiver);
                }
            }
            if (this._lookupTable.Count > 0)
            {
                ICollection[] array = new ICollection[this._ports.Length];
                int num = 0;
                foreach (Type key in this._types)
                {
                    array[num++] = this._lookupTable[key];
                }
                this.UnrollPartialCommit(array);
            }
        }

        public override bool Evaluate(IPortElement messageNode, ref ITask deferredTask)
        {
            throw new InvalidOperationException();
        }

        public override void Consume(IPortElement item)
        {
            throw new InvalidOperationException();
        }

        public override void Cleanup(ITask taskToCleanup)
        {
            ICollection[] results = (ICollection[])taskToCleanup[0].Item;
            this.UnrollPartialCommit(results);
        }

        private void UnrollPartialCommit(ICollection[] results)
        {
            foreach (ICollection collection in results)
            {
                IPort port = null;
                if (collection != null)
                {
                    foreach (object item in collection)
                    {
                        if (port == null)
                        {
                            foreach (IPort port2 in this._ports)
                            {
                                if (port2.TryPostUnknownType(item))
                                {
                                    port = port2;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            port.PostUnknownType(item);
                        }
                    }
                }
            }
        }

        private Handler<ICollection[]> _handler;

        private Type[] _types;

        private IPortReceive[] _ports;

        private Dictionary<Type, List<object>> _lookupTable = new Dictionary<Type, List<object>>();

        private Receiver[] _receivers;

        private int _expectedItemCount;

        private int _pendingItemCount;
    }
}
