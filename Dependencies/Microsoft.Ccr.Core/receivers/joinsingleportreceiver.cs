using System;
using Microsoft.Ccr.Core.Arbiters;
using Microsoft.Ccr.Core.Properties;

namespace Microsoft.Ccr.Core
{
    public class JoinSinglePortReceiver : JoinReceiverTask
    {
        internal JoinSinglePortReceiver()
        {}
        public JoinSinglePortReceiver(bool persist, ITask task, IPortReceive port, int count) 
            : base(task)
        {
            if (persist) _state = ReceiverTaskState.Persistent;
            if (count <= 0) 
                throw new ArgumentException(Resource.JoinSinglePortReceiverAtLeastOneItemMessage, nameof(count));
            _port = port;
            _count = count;
        }

        public override void Cleanup()
        {
            base.Cleanup();
            _port.UnregisterReceiver(this);
        }
        public override void Cleanup(ITask taskToCleanup)
        {
            if (taskToCleanup == null) throw new ArgumentNullException(nameof(taskToCleanup));
            for (var i = 0; i < _count; i++) ((IPortArbiterAccess)_port).PostElement(taskToCleanup[i]);
        }
        protected override void Register() => _port.RegisterReceiver(this);
        protected override bool ShouldCommit()
        {
            if (_state == ReceiverTaskState.CleanedUp) return false;
            if (_arbiter != null && _arbiter.ArbiterState != ArbiterTaskState.Active) return false;
            return _port.ItemCount >= _count;
        }
        public override bool Evaluate(IPortElement messageNode, ref ITask deferredTask)
        {
            deferredTask = null;
            if (ShouldCommit()) deferredTask = new Task(Commit);
            return false;
        }
        public override void Consume(IPortElement item)
        {
            if (ShouldCommit()) TaskQueue.Enqueue(new Task(Commit));
        }
        protected override void Commit()
        {
            var task = UserTask.PartialClone();
            var els = ((IPortArbiterAccess)_port).TestForMultipleElements(_count);
            if (els == null) return;
            for (var i = 0; i < _count; i++) task[i] = els[i];
            Arbitrate(task, els, true);
        }
        protected override void UnrollPartialCommit(IPortElement[] items)
        {
            var portArbiterAccess = (IPortArbiterAccess)_port;
            for (var i = 0; i < _count; i++)
                if (items[i] != null) 
                    portArbiterAccess.PostElement(items[i]);
        }

        private readonly IPortReceive _port;
        private readonly int _count;
    }
}
