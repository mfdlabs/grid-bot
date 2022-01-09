using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Ccr.Core.Arbiters;

namespace Microsoft.Ccr.Core
{
    public class MultipleItemGather : ReceiverTask, ITask
    {
        public MultipleItemGather(Type[] types, IPortReceive[] ports, int itemCount, Handler<ICollection[]> handler)
        {
            _state = ReceiverTaskState.Onetime;
            if (ports == null) throw new ArgumentNullException(nameof(ports));
            if (types == null) throw new ArgumentNullException(nameof(types));
            if (ports.Length == 0) throw new ArgumentOutOfRangeException(nameof(ports));
            if (types.Length == 0) throw new ArgumentOutOfRangeException(nameof(types));
            if (types.Length != ports.Length) 
                throw new ArgumentOutOfRangeException(nameof(types), "Type array length must match port array length");
            _types = types;
            _ports = ports;
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _pendingItemCount = itemCount;
            _expectedItemCount = itemCount;
            foreach (var key in _types) _lookupTable.Add(key, new List<object>());
        }

        public new ITask PartialClone() => new MultipleItemGather(_types, _ports, _pendingItemCount, _handler);

        public override IEnumerator<ITask> Execute()
        {
            base.Execute();
            Register();
            return null;
        }

        private void Register()
        {
            int num = 0;
            _receivers = new Receiver[_ports.Length];
            foreach (IPortReceive portReceive in _ports)
            {
                Receiver receiver = new GatherPrivateReceiver(portReceive, this);
                _receivers[num++] = receiver;
                receiver.TaskQueue = base.TaskQueue;
                portReceive.RegisterReceiver(receiver);
                if (_pendingItemCount <= 0)
                {
                    return;
                }
            }
        }

        internal bool Evaluate(object item, ref ITask deferredTask)
        {
            int num = Interlocked.Decrement(ref _pendingItemCount);
            if (num < 0 || _state == ReceiverTaskState.CleanedUp)
            {
                return false;
            }
            Type type = item.GetType();
            List<object> list;
            _lookupTable.TryGetValue(type, out list);
            while (list == null)
            {
                type = type.BaseType;
                if (type == null)
                {
                    break;
                }
                _lookupTable.TryGetValue(type, out list);
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
            ICollection[] array = new ICollection[_ports.Length];
            int num2 = 0;
            foreach (Type key in _types)
            {
                array[num2++] = _lookupTable[key];
            }
            deferredTask = new Task<ICollection[]>(array, _handler)
            {
                LinkedIterator = base.LinkedIterator,
                TaskQueue = base.TaskQueue,
                ArbiterCleanupHandler = base.ArbiterCleanupHandler
            };
            if (Arbiter == null)
            {
                _lookupTable.Clear();
                Cleanup();
                return true;
            }
            if (!Arbiter.Evaluate(this, ref deferredTask))
            {
                return false;
            }
            _lookupTable.Clear();
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
                Register();
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();
            foreach (Receiver receiver in _receivers)
            {
                if (receiver != null)
                {
                    receiver._port.UnregisterReceiver(receiver);
                }
            }
            if (_lookupTable.Count > 0)
            {
                ICollection[] array = new ICollection[_ports.Length];
                int num = 0;
                foreach (Type key in _types)
                {
                    array[num++] = _lookupTable[key];
                }
                UnrollPartialCommit(array);
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
            UnrollPartialCommit(results);
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
                            foreach (IPort port2 in _ports)
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
