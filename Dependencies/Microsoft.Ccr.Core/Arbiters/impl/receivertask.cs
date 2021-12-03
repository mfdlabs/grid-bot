using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Ccr.Core.Arbiters
{
    public abstract class ReceiverTask : TaskCommon
    {
        public override string ToString() 
            => string.Format(
                CultureInfo.InvariantCulture,
                "{0}({1}) with {2} nested under \n    {3}",
                GetType().Name,
                _state,
                _task == null ? "no continuation" : $"method {_task}",
                _arbiter?.ToString() ?? "none"
            );

        protected ReceiverTask() { }
        protected ReceiverTask(ITask taskToRun) => _task = taskToRun;

        public override ITask PartialClone() => throw new NotImplementedException();

        public object ArbiterContext
        {
            get => _arbiterContext;
            set => _arbiterContext = value;
        }
        public virtual IArbiterTask Arbiter
        {
            get => _arbiter;
            set => _arbiter = value;
        }
        public ReceiverTaskState State
        {
            get => _state;
            set => _state = value;
        }
        public override IPortElement this[int index]
        {
            get => _task[index];
            set => _task[index] = value;
        }
        public override int PortElementCount => _task?.PortElementCount ?? 0;

        public override IEnumerator<ITask> Execute() { Arbiter = null; return null; }

        protected ITask UserTask
        {
            get => _task;
            set => _task = value;
        }

        public abstract bool Evaluate(IPortElement messageNode, ref ITask deferredTask);
        public abstract void Consume(IPortElement item);
        public virtual void Cleanup() => _state = ReceiverTaskState.CleanedUp;
        public abstract void Cleanup(ITask taskToCleanup);

        private ITask _task;
        internal ReceiverTaskState _state;
        internal IArbiterTask _arbiter;
        internal object _arbiterContext;
    }
}
