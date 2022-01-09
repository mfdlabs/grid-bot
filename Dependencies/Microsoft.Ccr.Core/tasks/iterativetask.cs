using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Ccr.Core.Arbiters;

namespace Microsoft.Ccr.Core
{
    public class IterativeTask : TaskCommon
    {
        public IterativeTask(IteratorHandler handler)
        {
            Handler = handler;
            _causalityContext = Dispatcher.CloneCausalitiesFromCurrentThread();
        }

        public override ITask PartialClone() => new IterativeTask(Handler);

        public IteratorHandler Handler { get; }
        public override IPortElement this[int index]
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public override int PortElementCount => 0;

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        public override IEnumerator<ITask> Execute()
        {
            Dispatcher.SetCurrentThreadCausalities(_causalityContext);
            return Handler();
        }

        private readonly CausalityThreadContext _causalityContext;
    }

    public class IterativeTask<T0> : TaskCommon
    {
        public IterativeTask(IteratorHandler<T0> handler) => _handler = handler;

        public override ITask PartialClone() => new IterativeTask<T0>(_handler);

        public IterativeTask(T0 t0, IteratorHandler<T0> handler)
        {
            _handler = handler;
            _param0 = new PortElement<T0>(t0)
            {
                _causalityContext = Dispatcher.CloneCausalitiesFromCurrentThread()
            };
        }

        public override IPortElement this[int index]
        {
            get
            {
                if (index == 0) return _param0;
                throw new ArgumentException("parameter out of range", nameof(index));
            }
            set
            {
                if (index != 0) throw new ArgumentException("parameter out of range", nameof(index));
                _param0 = (PortElement<T0>)value;
            }
        }
        public override int PortElementCount => 1;

        public override string ToString()
        {
            if (_handler.Target == null) return "unknown:" + _handler.Method.Name;
            return _handler.Target + ":" + _handler.Method.Name;
        }
        [DebuggerStepThrough]
        [DebuggerNonUserCode]
        public override IEnumerator<ITask> Execute() => _handler(_param0.TypedItem);

        private readonly IteratorHandler<T0> _handler;
        private PortElement<T0> _param0;
    }

    public class IterativeTask<T0, T1> : TaskCommon
    {
        public IterativeTask(IteratorHandler<T0, T1> handler) => _handler = handler;

        public override ITask PartialClone() => new IterativeTask<T0, T1>(_handler);

        public IterativeTask(T0 t0, T1 t1, IteratorHandler<T0, T1> handler)
        {
            _handler = handler;
            _param0 = new PortElement<T0>(t0);
            _param1 = new PortElement<T1>(t1);
            _param0._causalityContext = Dispatcher.CloneCausalitiesFromCurrentThread();
        }

        public override IPortElement this[int index]
        {
            get
            {
                return index switch
                {
                    0 => _param0,
                    1 => _param1,
                    _ => throw new ArgumentException("parameter out of range", nameof(index))
                };
            }
            set
            {
                switch (index)
                {
                    case 0:
                        _param0 = (PortElement<T0>)value;
                        return;
                    case 1:
                        _param1 = (PortElement<T1>)value;
                        return;
                    default:
                        throw new ArgumentException("parameter out of range", nameof(index));
                }
            }
        }
        public override int PortElementCount => 2;

        public override string ToString()
        {
            if (_handler.Target == null) return "unknown:" + _handler.Method.Name;
            return _handler.Target + ":" + _handler.Method.Name;
        }

        [DebuggerStepThrough]
        [DebuggerNonUserCode]
        public override IEnumerator<ITask> Execute() => _handler(_param0.TypedItem, _param1.TypedItem);

        private readonly IteratorHandler<T0, T1> _handler;
        private PortElement<T0> _param0;
        private PortElement<T1> _param1;
    }

    public class IterativeTask<T0, T1, T2> : TaskCommon
    {
        public IterativeTask(IteratorHandler<T0, T1, T2> handler) => _handler = handler;

        public override ITask PartialClone() => new IterativeTask<T0, T1, T2>(_handler);

        public IterativeTask(T0 t0, T1 t1, T2 t2, IteratorHandler<T0, T1, T2> handler)
        {
            _handler = handler;
            _param0 = new PortElement<T0>(t0);
            _param1 = new PortElement<T1>(t1);
            _param2 = new PortElement<T2>(t2);
            _param0._causalityContext = Dispatcher.CloneCausalitiesFromCurrentThread();
        }

        public override IPortElement this[int index]
        {
            get
            {
                return index switch
                {
                    0 => _param0,
                    1 => _param1,
                    2 => _param2,
                    _ => throw new ArgumentException("parameter out of range", nameof(index))
                };
            }
            set
            {
                switch (index)
                {
                    case 0:
                        _param0 = (PortElement<T0>)value;
                        return;
                    case 1:
                        _param1 = (PortElement<T1>)value;
                        return;
                    case 2:
                        _param2 = (PortElement<T2>)value;
                        return;
                    default:
                        throw new ArgumentException("parameter out of range", nameof(index));
                }
            }
        }
        public override int PortElementCount => 3;

        public override string ToString()
        {
            if (_handler.Target == null) return "unknown:" + _handler.Method.Name;
            return _handler.Target + ":" + _handler.Method.Name;
        }

        [DebuggerStepThrough]
        [DebuggerNonUserCode]
        public override IEnumerator<ITask> Execute() => _handler(_param0.TypedItem, _param1.TypedItem, _param2.TypedItem);

        private readonly IteratorHandler<T0, T1, T2> _handler;
        private PortElement<T0> _param0;
        private PortElement<T1> _param1;
        private PortElement<T2> _param2;
    }
}
