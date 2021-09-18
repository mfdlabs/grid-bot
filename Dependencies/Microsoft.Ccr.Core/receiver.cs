using Microsoft.Ccr.Core.Arbiters;
using System;
using System.Diagnostics;

namespace Microsoft.Ccr.Core
{
    public class Receiver : ReceiverTask
    {
        internal bool KeepItemInPort
        {
            get
            {
                return this._keepItemInPort;
            }
            set
            {
                this._keepItemInPort = value;
            }
        }

        internal Receiver()
        {
        }

        internal Receiver(IPortReceive port)
        {
            this._port = port;
        }

        public Receiver(IPortReceive port, ITask task) : this(false, port, task)
        {
        }

        public Receiver(bool persist, IPortReceive port, ITask task) : base(task)
        {
            if (persist)
            {
                this._state = ReceiverTaskState.Persistent;
            }
            this._port = port;
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
                this._port.RegisterReceiver(this);
            }
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
            ((IPortArbiterAccess)this._port).PostElement(taskToCleanup[0]);
        }

        public override bool Evaluate(IPortElement messageNode, ref ITask deferredTask)
        {
            if (this._state == ReceiverTaskState.CleanedUp)
            {
                return false;
            }
            if (base.UserTask != null)
            {
                if (this._state == ReceiverTaskState.Persistent)
                {
                    deferredTask = base.UserTask.PartialClone();
                }
                else
                {
                    deferredTask = base.UserTask;
                }
                deferredTask[0] = messageNode;
            }
            else if (this._keepItemInPort)
            {
                deferredTask = new Task(new Handler(this.Cleanup));
            }
            if (this._arbiter != null)
            {
                bool flag = this._arbiter.Evaluate(this, ref deferredTask);
                return !this._keepItemInPort && flag;
            }
            if (deferredTask == null)
            {
                return true;
            }
            deferredTask.LinkedIterator = base.LinkedIterator;
            deferredTask.ArbiterCleanupHandler = base.ArbiterCleanupHandler;
            return !this._keepItemInPort;
        }

        public override void Consume(IPortElement item)
        {
            if (this._state == ReceiverTaskState.CleanedUp)
            {
                return;
            }
            ITask task = base.UserTask.PartialClone();
            task[0] = item;
            task.LinkedIterator = base.LinkedIterator;
            task.ArbiterCleanupHandler = base.ArbiterCleanupHandler;
            base.TaskQueue.Enqueue(task);
        }

        internal IPortReceive _port;

        private bool _keepItemInPort;
    }

    public class Receiver<T> : Receiver
    {
        internal Receiver()
        {
        }

        internal Receiver(IPortReceive port) : base(port)
        {
        }

        public Receiver(IPortReceive port, Predicate<T> predicate, Task<T> task) : this(false, port, predicate, task)
        {
        }

        public Receiver(bool persist, IPortReceive port, Predicate<T> predicate, Task<T> task) : base(persist, port, task)
        {
            this._predicate = predicate;
        }

        public Receiver(bool persist, IPortReceive port, Predicate<T> predicate, IterativeTask<T> task) : base(persist, port, task)
        {
            this._predicate = predicate;
        }

        public Receiver(IPortReceive port, Predicate<T> predicate, IterativeTask<T> task) : base(port, task)
        {
            this._predicate = predicate;
        }

        public Predicate<T> Predicate
        {
            get
            {
                return this._predicate;
            }
            set
            {
                this._predicate = value;
            }
        }

        public override bool Evaluate(IPortElement messageNode, ref ITask deferredTask)
        {
            if (this._predicate == null)
            {
                return base.Evaluate(messageNode, ref deferredTask);
            }
            bool result;
            try
            {
                if (this._predicate((T)((object)messageNode.Item)))
                {
                    result = base.Evaluate(messageNode, ref deferredTask);
                }
                else
                {
                    result = false;
                }
            }
            catch (Exception arg)
            {
                if (Dispatcher.TraceSwitchCore.TraceError)
                {
                    Trace.WriteLine("Predicate caused an exception, ignoring message. Exception:" + arg);
                }
                result = false;
            }
            return result;
        }

        private Predicate<T> _predicate;
    }
}
