using System.Collections.Generic;
using Microsoft.Ccr.Core.Arbiters;

namespace Microsoft.Ccr.Core
{
    public sealed class VariableArgumentTask<T> : ITask
    {
        public Handler ArbiterCleanupHandler
        {
            get => _ArbiterCleanupHandler;
            set => _ArbiterCleanupHandler = value;
        }
        public object LinkedIterator
        {
            get => _linkedIterator;
            set => _linkedIterator = value;
        }
        public DispatcherQueue TaskQueue
        {
            get => _dispatcherQueue;
            set => _dispatcherQueue = value;
        }

        public VariableArgumentTask(int varArgSize, VariableArgumentHandler<T> handler)
        {
            _Handler = handler;
            _aParams = new IPortElement[varArgSize];
        }

        public ITask PartialClone() => new VariableArgumentTask<T>(_aParams.Length, _Handler);

        public IPortElement this[int index]
        {
            get => _aParams[index];
            set => _aParams[index] = value;
        }
        public int PortElementCount => _aParams.Length;

        public override string ToString()
        {
            if (_Handler.Target == null) return $"unknown:{_Handler.Method.Name}";
            return $"{_Handler.Target}:{_Handler.Method.Name}";
        }
        public IEnumerator<ITask> Execute()
        {
            var argumentsLength = _aParams.Length;
            var @params = new T[argumentsLength];
            while (--argumentsLength >= 0) 
                @params[argumentsLength] = (T)_aParams[argumentsLength].Item;
            _Handler(@params);
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
            get => _ArbiterCleanupHandler;
            set => _ArbiterCleanupHandler = value;
        }
        public object LinkedIterator
        {
            get => _linkedIterator;
            set => _linkedIterator = value;
        }
        public DispatcherQueue TaskQueue
        {
            get => _dispatcherQueue;
            set => _dispatcherQueue = value;
        }

        public VariableArgumentTask(int varArgSize, VariableArgumentHandler<T0, T> handler)
        {
            _Handler = handler;
            _aParams = new IPortElement[varArgSize];
        }

        public ITask PartialClone() => new VariableArgumentTask<T0, T>(_aParams.Length, _Handler);

        public IPortElement this[int index]
        {
            get
            {
                if (index == 0)  return _Param0;
                return _aParams[index - 1];
            }
            set
            {
                if (index == 0)
                {
                    _Param0 = value;
                    return;
                }
                _aParams[index - 1] = value;
            }
        }
        public int PortElementCount => 1 + _aParams.Length;

        public override string ToString()
        {
            if (_Handler.Target == null) return $"unknown:{_Handler.Method.Name}";
            return $"{_Handler.Target}:{_Handler.Method.Name}";
        }
        public IEnumerator<ITask> Execute()
        {
            var argsLength = _aParams.Length;
            var @params = new T[argsLength];
            while (--argsLength >= 0) 
                @params[argsLength] = (T)(_aParams[argsLength].Item);
            _Handler((T0)_Param0.Item, @params);
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
