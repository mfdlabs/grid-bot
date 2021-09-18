using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Ccr.Core.Arbiters
{
    public abstract class ReceiverTask : TaskCommon
    {
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}({1}) with {2} nested under \n    {3}", new object[]
            {
                base.GetType().Name,
                this._state,
                (this._task == null) ? "no continuation" : ("method " + this._task.ToString()),
                (this._arbiter == null) ? "none" : this._arbiter.ToString()
            });
        }

        protected ReceiverTask()
        {
        }

        protected ReceiverTask(ITask taskToRun)
        {
            this._task = taskToRun;
        }

        public override ITask PartialClone()
        {
            throw new NotImplementedException();
        }

        public object ArbiterContext
        {
            get
            {
                return this._arbiterContext;
            }
            set
            {
                this._arbiterContext = value;
            }
        }

        public virtual IArbiterTask Arbiter
        {
            get
            {
                return this._arbiter;
            }
            set
            {
                this._arbiter = value;
            }
        }

        public ReceiverTaskState State
        {
            get
            {
                return this._state;
            }
            set
            {
                this._state = value;
            }
        }

        public override IPortElement this[int index]
        {
            get
            {
                return this._task[index];
            }
            set
            {
                this._task[index] = value;
            }
        }

        public override int PortElementCount
        {
            get
            {
                if (this._task == null)
                {
                    return 0;
                }
                return this._task.PortElementCount;
            }
        }

        public override IEnumerator<ITask> Execute()
        {
            this.Arbiter = null;
            return null;
        }

        protected ITask UserTask
        {
            get
            {
                return this._task;
            }
            set
            {
                this._task = value;
            }
        }

        public abstract bool Evaluate(IPortElement messageNode, ref ITask deferredTask);

        public abstract void Consume(IPortElement item);

        public virtual void Cleanup()
        {
            this._state = ReceiverTaskState.CleanedUp;
        }

        public abstract void Cleanup(ITask taskToCleanup);

        private ITask _task;

        internal ReceiverTaskState _state;

        internal IArbiterTask _arbiter;

        internal object _arbiterContext;
    }
}
