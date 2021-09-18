using Microsoft.Ccr.Core.Arbiters;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Ccr.Core
{
    public class IterativeTask : TaskCommon
    {
        public IterativeTask(IteratorHandler handler)
        {
            this._Handler = handler;
            this._causalityContext = Dispatcher.CloneCausalitiesFromCurrentThread();
        }

        public override ITask PartialClone()
        {
            return new IterativeTask(this._Handler);
        }

        public IteratorHandler Handler
        {
            get
            {
                return this._Handler;
            }
        }

        public override IPortElement this[int index]
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override int PortElementCount
        {
            get
            {
                return 0;
            }
        }

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        public override IEnumerator<ITask> Execute()
        {
            Dispatcher.SetCurrentThreadCausalities(this._causalityContext);
            return this._Handler();
        }

        private CausalityThreadContext _causalityContext;

        private IteratorHandler _Handler;
    }

    public class IterativeTask<T0> : TaskCommon
    {
        public IterativeTask(IteratorHandler<T0> handler)
        {
            this._Handler = handler;
        }

        public override ITask PartialClone()
        {
            return new IterativeTask<T0>(this._Handler);
        }

        public IterativeTask(T0 t0, IteratorHandler<T0> handler)
        {
            this._Handler = handler;
            this._Param0 = new PortElement<T0>(t0);
            this._Param0._causalityContext = Dispatcher.CloneCausalitiesFromCurrentThread();
        }

        public override IPortElement this[int index]
        {
            get
            {
                if (index == 0)
                {
                    return this._Param0;
                }
                throw new ArgumentException("parameter out of range", "index");
            }
            set
            {
                if (index == 0)
                {
                    this._Param0 = (PortElement<T0>)value;
                    return;
                }
                throw new ArgumentException("parameter out of range", "index");
            }
        }

        public override int PortElementCount
        {
            get
            {
                return 1;
            }
        }

        public override string ToString()
        {
            if (this._Handler.Target == null)
            {
                return "unknown:" + this._Handler.Method.Name;
            }
            return this._Handler.Target.ToString() + ":" + this._Handler.Method.Name;
        }

        [DebuggerStepThrough]
        [DebuggerNonUserCode]
        public override IEnumerator<ITask> Execute()
        {
            return this._Handler(this._Param0.TypedItem);
        }

        private readonly IteratorHandler<T0> _Handler;

        private PortElement<T0> _Param0;
    }

    public class IterativeTask<T0, T1> : TaskCommon
    {
        public IterativeTask(IteratorHandler<T0, T1> handler)
        {
            this._Handler = handler;
        }

        public override ITask PartialClone()
        {
            return new IterativeTask<T0, T1>(this._Handler);
        }

        public IterativeTask(T0 t0, T1 t1, IteratorHandler<T0, T1> handler)
        {
            this._Handler = handler;
            this._Param0 = new PortElement<T0>(t0);
            this._Param1 = new PortElement<T1>(t1);
            this._Param0._causalityContext = Dispatcher.CloneCausalitiesFromCurrentThread();
        }

        public override IPortElement this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return this._Param0;
                    case 1:
                        return this._Param1;
                    default:
                        throw new ArgumentException("parameter out of range", "index");
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        this._Param0 = (PortElement<T0>)value;
                        return;
                    case 1:
                        this._Param1 = (PortElement<T1>)value;
                        return;
                    default:
                        throw new ArgumentException("parameter out of range", "index");
                }
            }
        }

        public override int PortElementCount
        {
            get
            {
                return 2;
            }
        }

        public override string ToString()
        {
            if (this._Handler.Target == null)
            {
                return "unknown:" + this._Handler.Method.Name;
            }
            return this._Handler.Target.ToString() + ":" + this._Handler.Method.Name;
        }

        [DebuggerStepThrough]
        [DebuggerNonUserCode]
        public override IEnumerator<ITask> Execute()
        {
            return this._Handler(this._Param0.TypedItem, this._Param1.TypedItem);
        }

        private readonly IteratorHandler<T0, T1> _Handler;

        private PortElement<T0> _Param0;

        private PortElement<T1> _Param1;
    }

    public class IterativeTask<T0, T1, T2> : TaskCommon
    {
        public IterativeTask(IteratorHandler<T0, T1, T2> handler)
        {
            this._Handler = handler;
        }

        public override ITask PartialClone()
        {
            return new IterativeTask<T0, T1, T2>(this._Handler);
        }

        public IterativeTask(T0 t0, T1 t1, T2 t2, IteratorHandler<T0, T1, T2> handler)
        {
            this._Handler = handler;
            this._Param0 = new PortElement<T0>(t0);
            this._Param1 = new PortElement<T1>(t1);
            this._Param2 = new PortElement<T2>(t2);
            this._Param0._causalityContext = Dispatcher.CloneCausalitiesFromCurrentThread();
        }

        public override IPortElement this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return this._Param0;
                    case 1:
                        return this._Param1;
                    case 2:
                        return this._Param2;
                    default:
                        throw new ArgumentException("parameter out of range", "index");
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        this._Param0 = (PortElement<T0>)value;
                        return;
                    case 1:
                        this._Param1 = (PortElement<T1>)value;
                        return;
                    case 2:
                        this._Param2 = (PortElement<T2>)value;
                        return;
                    default:
                        throw new ArgumentException("parameter out of range", "index");
                }
            }
        }

        public override int PortElementCount
        {
            get
            {
                return 3;
            }
        }

        public override string ToString()
        {
            if (this._Handler.Target == null)
            {
                return "unknown:" + this._Handler.Method.Name;
            }
            return this._Handler.Target.ToString() + ":" + this._Handler.Method.Name;
        }

        [DebuggerStepThrough]
        [DebuggerNonUserCode]
        public override IEnumerator<ITask> Execute()
        {
            return this._Handler(this._Param0.TypedItem, this._Param1.TypedItem, this._Param2.TypedItem);
        }

        private readonly IteratorHandler<T0, T1, T2> _Handler;

        private PortElement<T0> _Param0;

        private PortElement<T1> _Param1;

        private PortElement<T2> _Param2;
    }
}
