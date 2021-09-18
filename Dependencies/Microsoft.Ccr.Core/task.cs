using Microsoft.Ccr.Core.Arbiters;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Ccr.Core
{
    public class Task : TaskCommon
    {
        public Task(Handler handler)
        {
            this._handler = handler;
            this._causalityContext = Dispatcher.CloneCausalitiesFromCurrentThread();
        }

        public Handler Handler
        {
            get
            {
                return this._handler;
            }
        }

        public override ITask PartialClone()
        {
            return new Task(this._handler);
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

        [DebuggerStepThrough]
        [DebuggerNonUserCode]
        public override IEnumerator<ITask> Execute()
        {
            Dispatcher.SetCurrentThreadCausalities(this._causalityContext);
            this._handler();
            return null;
        }

        private CausalityThreadContext _causalityContext;

        private Handler _handler;
    }

    public class Task<T0> : TaskCommon
    {
        public Task(Handler<T0> handler)
        {
            this._Handler = handler;
        }

        public override string ToString()
        {
            if (this._Handler.Target == null)
            {
                return "unknown:" + this._Handler.Method.Name;
            }
            return this._Handler.Target.ToString() + ":" + this._Handler.Method.Name;
        }

        public override ITask PartialClone()
        {
            return new Task<T0>(this._Handler);
        }

        public Task(T0 t0, Handler<T0> handler)
        {
            this._Handler = handler;
            this.Param0 = new PortElement<T0>(t0);
            this.Param0._causalityContext = Dispatcher.CloneCausalitiesFromCurrentThread();
        }

        public override IPortElement this[int index]
        {
            get
            {
                if (index == 0)
                {
                    return this.Param0;
                }
                throw new ArgumentException("parameter out of range", "index");
            }
            set
            {
                if (index == 0)
                {
                    this.Param0 = (PortElement<T0>)value;
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

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        public override IEnumerator<ITask> Execute()
        {
            this._Handler(this.Param0.TypedItem);
            return null;
        }

        private readonly Handler<T0> _Handler;

        protected PortElement<T0> Param0;
    }

    public class Task<T0, T1> : TaskCommon
    {
        public Task(Handler<T0, T1> handler)
        {
            this._Handler = handler;
        }

        public override string ToString()
        {
            if (this._Handler.Target == null)
            {
                return "unknown:" + this._Handler.Method.Name;
            }
            return this._Handler.Target.ToString() + ":" + this._Handler.Method.Name;
        }

        public override ITask PartialClone()
        {
            return new Task<T0, T1>(this._Handler);
        }

        public Task(T0 t0, T1 t1, Handler<T0, T1> handler)
        {
            this._Handler = handler;
            this.Param0 = new PortElement<T0>(t0);
            this.Param1 = new PortElement<T1>(t1);
            this.Param0._causalityContext = Dispatcher.CloneCausalitiesFromCurrentThread();
        }

        public override IPortElement this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return this.Param0;
                    case 1:
                        return this.Param1;
                    default:
                        throw new ArgumentException("parameter out of range", "index");
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        this.Param0 = (PortElement<T0>)value;
                        return;
                    case 1:
                        this.Param1 = (PortElement<T1>)value;
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

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        public override IEnumerator<ITask> Execute()
        {
            this._Handler(this.Param0.TypedItem, this.Param1.TypedItem);
            return null;
        }

        private readonly Handler<T0, T1> _Handler;

        protected PortElement<T0> Param0;

        protected PortElement<T1> Param1;
    }

    public class Task<T0, T1, T2> : TaskCommon
    {
        public Task(Handler<T0, T1, T2> handler)
        {
            this._Handler = handler;
        }

        public override string ToString()
        {
            if (this._Handler.Target == null)
            {
                return "unknown:" + this._Handler.Method.Name;
            }
            return this._Handler.Target.ToString() + ":" + this._Handler.Method.Name;
        }

        public override ITask PartialClone()
        {
            return new Task<T0, T1, T2>(this._Handler);
        }

        public Task(T0 t0, T1 t1, T2 t2, Handler<T0, T1, T2> handler)
        {
            this._Handler = handler;
            this.Param0 = new PortElement<T0>(t0);
            this.Param1 = new PortElement<T1>(t1);
            this.Param2 = new PortElement<T2>(t2);
            this.Param0._causalityContext = Dispatcher.CloneCausalitiesFromCurrentThread();
        }

        public override IPortElement this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return this.Param0;
                    case 1:
                        return this.Param1;
                    case 2:
                        return this.Param2;
                    default:
                        throw new ArgumentException("parameter out of range", "index");
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        this.Param0 = (PortElement<T0>)value;
                        return;
                    case 1:
                        this.Param1 = (PortElement<T1>)value;
                        return;
                    case 2:
                        this.Param2 = (PortElement<T2>)value;
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

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        public override IEnumerator<ITask> Execute()
        {
            this._Handler(this.Param0.TypedItem, this.Param1.TypedItem, this.Param2.TypedItem);
            return null;
        }

        private readonly Handler<T0, T1, T2> _Handler;

        protected PortElement<T0> Param0;

        protected PortElement<T1> Param1;

        protected PortElement<T2> Param2;
    }
}
