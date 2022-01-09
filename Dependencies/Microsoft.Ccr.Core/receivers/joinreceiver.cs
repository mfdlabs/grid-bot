using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Ccr.Core.Arbiters;
using Microsoft.Ccr.Core.Properties;

namespace Microsoft.Ccr.Core
{
    public class JoinReceiver : JoinReceiverTask
    {
        public override string ToString()
        {
            var builder = new StringBuilder();
            foreach (var port in _ports)
                builder.AppendFormat(
                    CultureInfo.InvariantCulture, 
                    "[{0}({1})] ", 
                    port._port.GetType().ToString(),
                    port._port.ItemCount.ToString(CultureInfo.InvariantCulture)
                );
            return string.Format(
                CultureInfo.InvariantCulture,
                "\t{0}({1}) waiting on ports {2} with {3} nested under \n    {4}",
                GetType().Name,
                _state,
                builder.ToString(),
                UserTask == null ? "no continuation" : "method " + UserTask,
                _arbiter == null ? "none" : _arbiter.ToString()
            );
        }

        internal JoinReceiver()
        {}
        public JoinReceiver(bool persist, ITask task, params IPortReceive[] ports) : base(task)
        {
            if (ports == null) throw new ArgumentNullException(nameof(ports));
            if (persist) _state = ReceiverTaskState.Persistent;
            if (ports == null || ports.Length == 0) 
                throw new ArgumentOutOfRangeException(nameof(ports), Resource.JoinsMustHaveOnePortMinimumException);
            _ports = new Receiver[ports.Length];
            var hashCodes = new int[ports.Length];
            var idx = 0;
            foreach (var portReceive in ports)
            {
                var hashCode = portReceive.GetHashCode();
                var receiver = new Receiver(portReceive);
                _ports[idx] = receiver;
                hashCodes[idx] = hashCode;
                receiver.ArbiterContext = idx;
                idx++;
            }
            Array.Sort(hashCodes, _ports);
        }

        public override void Cleanup()
        {
            base.Cleanup();
            foreach (var receiver in _ports) 
                receiver.Cleanup();
        }
        public override void Cleanup(ITask taskToCleanup)
        {
            if (taskToCleanup == null) throw new ArgumentNullException(nameof(taskToCleanup));
            foreach (var receiver in _ports) 
                ((IPortArbiterAccess)receiver._port).PostElement(taskToCleanup[(int)receiver.ArbiterContext]);
        }
        protected override void Register()
        {
            foreach (var port in _ports)
            {
                if (_state == ReceiverTaskState.Persistent) port._state = ReceiverTaskState.Persistent;
                port.Arbiter = this;
            }
        }
        public override bool Evaluate(IPortElement messageNode, ref ITask deferredTask)
        {
            deferredTask = null;
            return false;
        }
        public override void Consume(IPortElement item)
        {}
        protected override bool ShouldCommit()
        {
            if (_state == ReceiverTaskState.CleanedUp) return false;
            if (_arbiter != null && _arbiter.ArbiterState != ArbiterTaskState.Active) return false;
            return _ports.All(receiver => receiver._port.ItemCount != 0);
        }
        protected override void Commit()
        {
            if (!ShouldCommit()) return;
            var task = UserTask.PartialClone();
            var els = new IPortElement[_ports.Length];
            var allTaken = true;
            for (var i = 0; i < _ports.Length; i++)
            {
                var p = _ports[i];
                var el = ((IPortArbiterAccess)p._port).TestForElement();
                if (el == null)
                {
                    allTaken = false;
                    break;
                }
                els[i] = el;
                task[(int)p.ArbiterContext] = el;
            }
            Arbitrate(task, els, allTaken);
        }
        protected override void UnrollPartialCommit(IPortElement[] items)
        {
            for (var i = 0; i < _ports.Length; i++)
                if (items[i] != null) 
                    ((IPortArbiterAccess)items[i].Owner).PostElement(items[i]);
        }

        private readonly Receiver[] _ports;
    }
}
