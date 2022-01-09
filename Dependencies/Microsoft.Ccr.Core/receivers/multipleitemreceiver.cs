using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Ccr.Core.Arbiters;

namespace Microsoft.Ccr.Core
{
    public class MultipleItemReceiver : ReceiverTask
    {
        public MultipleItemReceiver(ITask userTask, params IPortReceive[] ports)
        {
            if (ports == null) throw new ArgumentNullException(nameof(ports));
            if (ports.Length == 0) throw new ArgumentOutOfRangeException(nameof(ports));
            _ports = ports;
            _userTask = userTask ?? throw new ArgumentNullException(nameof(userTask));
            _pendingItemCount = ports.Length;
            _receivers = new Receiver[_ports.Length];
        }

        public new ITask PartialClone() => new MultipleItemReceiver(_userTask.PartialClone(), _ports);
        public override IEnumerator<ITask> Execute()
        {
            base.Execute();
            return null;
        }
        private void Register()
        {
            var idx = 0;
            foreach (var port in _ports)
            {
                Receiver receiver = new MultipleItemHelperReceiver(port, this);
                receiver._arbiterContext = idx;
                _receivers[idx++] = receiver;
                receiver.TaskQueue = TaskQueue;
            }
            idx = 0;
            foreach (var port in _ports) port.RegisterReceiver(_receivers[idx++]);
        }
        internal bool Evaluate(int index, IPortElement item, ref ITask deferredTask)
        {
            if (State == ReceiverTaskState.CleanedUp) return false;
            if (_userTask[index] != null) throw new InvalidOperationException();
            _userTask[index] = item;
            var count = Interlocked.Decrement(ref _pendingItemCount);
            switch (count)
            {
                case > 0:
                    return true;
                case 0:
                {
                    _userTask.LinkedIterator = LinkedIterator;
                    _userTask.TaskQueue = TaskQueue;
                    _userTask.ArbiterCleanupHandler = ArbiterCleanupHandler;
                    deferredTask = _userTask;
                    if (Arbiter == null) return true;
                    if (!Arbiter.Evaluate(this, ref deferredTask)) return false;
                    _userTask = null;
                    return true;
                }
                default:
                    return false;
            }
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

        public override bool Evaluate(IPortElement messageNode, ref ITask deferredTask) => throw new NotImplementedException();
        public override void Consume(IPortElement item) => throw new NotImplementedException();
        public override void Cleanup()
        {
            State = ReceiverTaskState.CleanedUp;
            foreach (var receiver in _receivers) 
                receiver?._port.UnregisterReceiver(receiver);
            if (_userTask != null) Cleanup(_userTask);
        }
        public override void Cleanup(ITask taskToCleanup)
        {
            for (var i = 0; i < _ports.Length; i++)
            {
                var portElement = taskToCleanup[i];
                if (portElement != null) 
                    ((IPort)_ports[i]).TryPostUnknownType(taskToCleanup[i].Item);
            }
        }

        private ITask _userTask;
        private readonly IPortReceive[] _ports;
        private readonly Receiver[] _receivers;
        private int _pendingItemCount;
    }
}
