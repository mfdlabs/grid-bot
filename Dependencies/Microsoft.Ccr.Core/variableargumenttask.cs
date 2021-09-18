using Microsoft.Ccr.Core.Arbiters;
using System.Collections.Generic;

namespace Microsoft.Ccr.Core
{
    public sealed class VariableArgumentTask<T> : ITask
    {
        public Handler ArbiterCleanupHandler
        {
            get
            {
                return this._ArbiterCleanupHandler;
            }
            set
            {
                this._ArbiterCleanupHandler = value;
            }
        }

        public object LinkedIterator
        {
            get
            {
                return this._linkedIterator;
            }
            set
            {
                this._linkedIterator = value;
            }
        }

        public DispatcherQueue TaskQueue
        {
            get
            {
                return this._dispatcherQueue;
            }
            set
            {
                this._dispatcherQueue = value;
            }
        }

        public VariableArgumentTask(int varArgSize, VariableArgumentHandler<T> handler)
        {
            this._Handler = handler;
            this._aParams = new IPortElement[varArgSize];
        }

        public ITask PartialClone()
        {
            return new VariableArgumentTask<T>(this._aParams.Length, this._Handler);
        }

        public IPortElement this[int index]
        {
            get
            {
                return this._aParams[index];
            }
            set
            {
                this._aParams[index] = value;
            }
        }

        public int PortElementCount
        {
            get
            {
                return this._aParams.Length;
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

        public IEnumerator<ITask> Execute()
        {
            int num = this._aParams.Length;
            T[] array = new T[num];
            while (--num >= 0)
            {
                array[num] = (T)((object)this._aParams[num].Item);
            }
            this._Handler(array);
            return null;
        }

        private Handler _ArbiterCleanupHandler;

        private object _linkedIterator;

        private DispatcherQueue _dispatcherQueue;

        private readonly VariableArgumentHandler<T> _Handler;

        private readonly IPortElement[] _aParams;
    }

    public sealed class VariableArgumentTask<T0, T> : ITask
    {
        public Handler ArbiterCleanupHandler
        {
            get
            {
                return this._ArbiterCleanupHandler;
            }
            set
            {
                this._ArbiterCleanupHandler = value;
            }
        }

        public object LinkedIterator
        {
            get
            {
                return this._linkedIterator;
            }
            set
            {
                this._linkedIterator = value;
            }
        }

        public DispatcherQueue TaskQueue
        {
            get
            {
                return this._dispatcherQueue;
            }
            set
            {
                this._dispatcherQueue = value;
            }
        }

        public VariableArgumentTask(int varArgSize, VariableArgumentHandler<T0, T> handler)
        {
            this._Handler = handler;
            this._aParams = new IPortElement[varArgSize];
        }

        public ITask PartialClone()
        {
            return new VariableArgumentTask<T0, T>(this._aParams.Length, this._Handler);
        }

        public IPortElement this[int index]
        {
            get
            {
                if (index == 0)
                {
                    return this._Param0;
                }
                return this._aParams[index - 1];
            }
            set
            {
                if (index == 0)
                {
                    this._Param0 = value;
                    return;
                }
                this._aParams[index - 1] = value;
            }
        }

        public int PortElementCount
        {
            get
            {
                return 1 + this._aParams.Length;
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

        public IEnumerator<ITask> Execute()
        {
            int num = this._aParams.Length;
            T[] array = new T[num];
            while (--num >= 0)
            {
                array[num] = (T)((object)this._aParams[num].Item);
            }
            this._Handler((T0)((object)this._Param0.Item), array);
            return null;
        }

        private Handler _ArbiterCleanupHandler;

        private object _linkedIterator;

        private DispatcherQueue _dispatcherQueue;

        private readonly VariableArgumentHandler<T0, T> _Handler;

        private IPortElement _Param0;

        private readonly IPortElement[] _aParams;
    }
}
