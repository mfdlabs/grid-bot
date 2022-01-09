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
            var idx = 0;
            _receivers = new Receiver[_ports.Length];
            foreach (var port in _ports)
            {
                Receiver receiver = new GatherPrivateReceiver(port, this);
                _receivers[idx++] = receiver;
                receiver.TaskQueue = TaskQueue;
                port.RegisterReceiver(receiver);
                if (_pendingItemCount <= 0) return;
            }
        }
        internal bool Evaluate(object item, ref ITask deferredTask)
        {
            var count = Interlocked.Decrement(ref _pendingItemCount);
            if (count < 0 || _state == ReceiverTaskState.CleanedUp) return false;
            var type = item.GetType();
            _lookupTable.TryGetValue(type, out var lookup);
            while (lookup == null)
            {
                type = type.BaseType;
                if (type == null) break;
                _lookupTable.TryGetValue(type, out lookup);
            }
            if (lookup == null) throw new InvalidOperationException("No result collection found for type:" + type);
            lock (lookup) lookup.Add(item);
            if (count != 0) return true;
            var ports = new ICollection[_ports.Length];
            var idx = 0;
            foreach (var t in _types) ports[idx++] = _lookupTable[t];
            deferredTask = new Task<ICollection[]>(ports, _handler)
            {
                LinkedIterator = LinkedIterator,
                TaskQueue = TaskQueue,
                ArbiterCleanupHandler = ArbiterCleanupHandler
            };
            if (Arbiter == null)
            {
                _lookupTable.Clear();
                Cleanup();
                return true;
            }
            if (!Arbiter.Evaluate(this, ref deferredTask)) return false;
            _lookupTable.Clear();
            return true;
        }
        
        public override IArbiterTask Arbiter
        {
            set
            {
                base.Arbiter = value;
                TaskQueue ??= base.Arbiter.TaskQueue;
                Register();
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();
            foreach (var receiver in _receivers) receiver?._port.UnregisterReceiver(receiver);
            if (_lookupTable.Count <= 0) return;
            var ports = new ICollection[_ports.Length];
            var idx = 0;
            foreach (var t in _types) ports[idx++] = _lookupTable[t];
            UnrollPartialCommit(ports);
        }
        public override bool Evaluate(IPortElement messageNode, ref ITask deferredTask) 
            => throw new InvalidOperationException();
        public override void Consume(IPortElement item) => throw new InvalidOperationException();
        public override void Cleanup(ITask taskToCleanup) => UnrollPartialCommit((ICollection[])taskToCleanup[0].Item);
        private void UnrollPartialCommit(IEnumerable<ICollection> results)
        {
            foreach (var collection in results)
            {
                IPort port = null;
                if (collection == null) continue;
                foreach (var item in collection)
                {
                    if (port == null)
                    {
                        foreach (var portReceive in _ports)
                        {
                            var p = (IPort) portReceive;
                            if (!p.TryPostUnknownType(item)) continue;
                            port = p;
                            break;
                        }
                    }
                    else
                        port.PostUnknownType(item);
                }
            }
        }

        private readonly Handler<ICollection[]> _handler;
        private readonly Type[] _types;
        private readonly IPortReceive[] _ports;
        private readonly Dictionary<Type, List<object>> _lookupTable = new();
        private Receiver[] _receivers;
        private int _pendingItemCount;
    }
}
