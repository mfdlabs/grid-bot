using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Ccr.Core.Arbiters;

namespace Microsoft.Ccr.Core
{
    public class Task : TaskCommon
    {
        public Task(Handler handler)
        {
            _handler = handler;
            _causalityContext = Dispatcher.CloneCausalitiesFromCurrentThread();
        }

        public Handler Handler => _handler;
        public override IPortElement this[int index]
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public override int PortElementCount => 0;

        [DebuggerStepThrough]
        [DebuggerNonUserCode]
        public override IEnumerator<ITask> Execute()
        {
            Dispatcher.SetCurrentThreadCausalities(_causalityContext);
            _handler();
            return null;
        }
        public override ITask PartialClone() => new Task(_handler);

        private readonly CausalityThreadContext _causalityContext;
        private readonly Handler _handler;
    }

    public class Task<T0> : TaskCommon
    {
        public Task(Handler<T0> handler) => _Handler = handler;
        public Task(T0 t0, Handler<T0> handler)
        {
            _Handler = handler;
            Param0 = new PortElement<T0>(t0)
            {
                _causalityContext = Dispatcher.CloneCausalitiesFromCurrentThread()
            };
        }

        public override IPortElement this[int index]
        {
            get
            {
                if (index == 0) return Param0;
                throw new ArgumentException("parameter out of range", nameof(index));
            }
            set
            {
                if (index == 0)
                {
                    Param0 = (PortElement<T0>)value;
                    return;
                }
                throw new ArgumentException("parameter out of range", nameof(index));
            }
        }
        public override int PortElementCount => 1;

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        public override IEnumerator<ITask> Execute()
        {
            _Handler(Param0.TypedItem);
            return null;
        }
        public override ITask PartialClone() => new Task<T0>(_Handler);
        public override string ToString()
        {
            if (_Handler.Target == null) return $"unknown:{_Handler.Method.Name}";
            return $"{_Handler.Target}:{_Handler.Method.Name}";
        }

        private readonly Handler<T0> _Handler;
        protected PortElement<T0> Param0;
    }

    public class Task<T0, T1> : TaskCommon
    {
        public Task(Handler<T0, T1> handler) => _Handler = handler;
        public Task(T0 t0, T1 t1, Handler<T0, T1> handler)
        {
            _Handler = handler;
            Param0 = new PortElement<T0>(t0)
            {
                _causalityContext = Dispatcher.CloneCausalitiesFromCurrentThread()
            };
            Param1 = new PortElement<T1>(t1);
        }

        public override IPortElement this[int index]
        {
            get
            {
                return index switch
                {
                    0 => Param0,
                    1 => Param1,
                    _ => throw new ArgumentException("parameter out of range", nameof(index)),
                };
            }
            set
            {
                switch (index)
                {
                    case 0:
                        Param0 = (PortElement<T0>)value;
                        return;
                    case 1:
                        Param1 = (PortElement<T1>)value;
                        return;
                    default:
                        throw new ArgumentException("parameter out of range", nameof(index));
                }
            }
        }
        public override int PortElementCount => 2;

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        public override IEnumerator<ITask> Execute()
        {
            _Handler(Param0.TypedItem, Param1.TypedItem);
            return null;
        }
        public override ITask PartialClone() => new Task<T0, T1>(_Handler);
        public override string ToString()
        {
            if (_Handler.Target == null) return $"unknown:{_Handler.Method.Name}";
            return $"{_Handler.Target}:{_Handler.Method.Name}";
        }

        private readonly Handler<T0, T1> _Handler;
        protected PortElement<T0> Param0;
        protected PortElement<T1> Param1;
    }

    public class Task<T0, T1, T2> : TaskCommon
    {
        public Task(Handler<T0, T1, T2> handler) => _Handler = handler;
        public Task(T0 t0, T1 t1, T2 t2, Handler<T0, T1, T2> handler)
        {
            _Handler = handler;
            Param0 = new PortElement<T0>(t0)
            {
                _causalityContext = Dispatcher.CloneCausalitiesFromCurrentThread()
            };
            Param1 = new PortElement<T1>(t1);
            Param2 = new PortElement<T2>(t2);
        }

        public override IPortElement this[int index]
        {
            get
            {
                return index switch
                {
                    0 => Param0,
                    1 => Param1,
                    2 => Param2,
                    _ => throw new ArgumentException("parameter out of range", nameof(index)),
                };
            }
            set
            {
                switch (index)
                {
                    case 0:
                        Param0 = (PortElement<T0>)value;
                        return;
                    case 1:
                        Param1 = (PortElement<T1>)value;
                        return;
                    case 2:
                        Param2 = (PortElement<T2>)value;
                        return;
                    default:
                        throw new ArgumentException("parameter out of range", nameof(index));
                }
            }
        }
        public override int PortElementCount => 3;

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        public override IEnumerator<ITask> Execute()
        {
            _Handler(Param0.TypedItem, Param1.TypedItem, Param2.TypedItem);
            return null;
        }
        public override ITask PartialClone() => new Task<T0, T1, T2>(_Handler);
        public override string ToString()
        {
            if (_Handler.Target == null) return $"unknown:{_Handler.Method.Name}";
            return $"{_Handler.Target}:{_Handler.Method.Name}";
        }

        private readonly Handler<T0, T1, T2> _Handler;
        protected PortElement<T0> Param0;
        protected PortElement<T1> Param1;
        protected PortElement<T2> Param2;
    }
}
