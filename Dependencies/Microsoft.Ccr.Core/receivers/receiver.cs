using System;
using System.Diagnostics;
using Microsoft.Ccr.Core.Arbiters;

namespace Microsoft.Ccr.Core
{
    public class Receiver : ReceiverTask
    {
        internal bool KeepItemInPort
        {
            get => _keepItemInPort;
            set => _keepItemInPort = value;
        }

        internal Receiver() { }
        internal Receiver(IPortReceive port) => _port = port;
        public Receiver(IPortReceive port, ITask task) 
            : this(false, port, task)
        { }
        public Receiver(bool persist, IPortReceive port, ITask task) 
            : base(task)
        {
            if (persist) _state = ReceiverTaskState.Persistent;
            _port = port;
        }

        public override IArbiterTask Arbiter
        {
            set
            {
                base.Arbiter = value;
                if (TaskQueue == null) 
                    TaskQueue = base.Arbiter.TaskQueue;
                _port.RegisterReceiver(this);
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();
            _port.UnregisterReceiver(this);
        }
        public override void Cleanup(ITask taskToCleanup)
        {
            if (taskToCleanup == null) throw new ArgumentNullException(nameof(taskToCleanup));
            ((IPortArbiterAccess)_port).PostElement(taskToCleanup[0]);
        }
        public override bool Evaluate(IPortElement messageNode, ref ITask deferredTask)
        {
            if (_state == ReceiverTaskState.CleanedUp) return false;

            if (UserTask != null)
            {
                if (_state == ReceiverTaskState.Persistent) 
                    deferredTask = UserTask.PartialClone();
                else
                    deferredTask =UserTask;
                deferredTask[0] = messageNode;
            }
            else if (_keepItemInPort)
                deferredTask = new Task(Cleanup);
            if (_arbiter != null)
                return !_keepItemInPort && _arbiter.Evaluate(this, ref deferredTask);

            if (deferredTask == null) return true;

            deferredTask.LinkedIterator = LinkedIterator;
            deferredTask.ArbiterCleanupHandler = ArbiterCleanupHandler;
            return !_keepItemInPort;
        }
        public override void Consume(IPortElement item)
        {
            if (_state == ReceiverTaskState.CleanedUp) return;

            var userTask = UserTask.PartialClone();
            userTask[0] = item;
            userTask.LinkedIterator = base.LinkedIterator;
            userTask.ArbiterCleanupHandler = base.ArbiterCleanupHandler;
            TaskQueue.Enqueue(userTask);
        }

        internal IPortReceive _port;
        private bool _keepItemInPort;
    }

    public class Receiver<T> : Receiver
    {
        internal Receiver() { }
        internal Receiver(IPortReceive port) 
            : base(port)
        { }
        public Receiver(IPortReceive port, Predicate<T> predicate, Task<T> task)
            : this(false, port, predicate, task)
        { }
        public Receiver(bool persist, IPortReceive port, Predicate<T> predicate, Task<T> task)
            : base(persist, port, task) => _predicate = predicate;
        public Receiver(bool persist, IPortReceive port, Predicate<T> predicate, IterativeTask<T> task)
            : base(persist, port, task) => _predicate = predicate;
        public Receiver(IPortReceive port, Predicate<T> predicate, IterativeTask<T> task)
            : base(port, task) => _predicate = predicate;

        public Predicate<T> Predicate
        {
            get => _predicate;
            set => _predicate = value;
        }

        public override bool Evaluate(IPortElement messageNode, ref ITask deferredTask)
        {
            if (_predicate == null) return base.Evaluate(messageNode, ref deferredTask);

            try
            {
                if (_predicate((T)messageNode.Item)) return base.Evaluate(messageNode, ref deferredTask);
                else return false;
            }
            catch (Exception ex)
            {
                if (Dispatcher.TraceSwitchCore.TraceError) 
                    Trace.WriteLine($"Predicate caused an exception, ignoring message. Exception:{ex}");
                return false;
            }
        }

        private Predicate<T> _predicate;
    }
}
