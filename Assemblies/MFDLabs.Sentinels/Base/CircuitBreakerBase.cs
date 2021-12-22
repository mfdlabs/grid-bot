using System;
using System.Threading;

namespace MFDLabs.Sentinels
{
    public abstract class CircuitBreakerBase : ICircuitBreaker
    {
        protected internal abstract string Name { get; }
        protected internal DateTime? Tripped { get; private set; }
        protected virtual DateTime Now => DateTime.UtcNow;
        public bool IsTripped => _isTripped == 1;

        public virtual bool Reset()
        {
            var tripped = Interlocked.Exchange(ref _isTripped, 0) == 1;
            if (tripped) Tripped = null;
            return tripped;
        }
        public virtual void Test() { if (IsTripped) throw new CircuitBreakerException(this); }
        public virtual bool Trip()
        {
            var rripped = Interlocked.Exchange(ref _isTripped, 1) == 1;
            if (!rripped) Tripped = Now;
            return rripped;
        }

        private int _isTripped;
    }
}
