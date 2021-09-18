using System;
using System.Threading;

namespace MFDLabs.Sentinels
{
    public abstract class CircuitBreakerBase : ICircuitBreaker
    {
        protected internal abstract string Name { get; }

        protected internal DateTime? Tripped { get; private set; }

        protected virtual DateTime Now
        {
            get
            {
                return DateTime.UtcNow;
            }
        }

        public bool IsTripped
        {
            get
            {
                return _IsTripped == 1;
            }
        }

        public virtual bool Reset()
        {
            var exchanged = Interlocked.Exchange(ref _IsTripped, 0) == 1;
            if (exchanged)
            {
                Tripped = null;
            }
            return exchanged;
        }

        public virtual void Test()
        {
            if (IsTripped)
            {
                throw new CircuitBreakerException(this);
            }
        }

        public virtual bool Trip()
        {
            var exchanged = Interlocked.Exchange(ref _IsTripped, 1) == 1;
            if (!exchanged)
            {
                Tripped = Now;
            }
            return exchanged;
        }

        private int _IsTripped;
    }
}
