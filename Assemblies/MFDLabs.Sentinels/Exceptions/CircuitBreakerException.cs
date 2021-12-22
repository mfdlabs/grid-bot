using System;

namespace MFDLabs.Sentinels
{
    public class CircuitBreakerException : Exception
    {
        public override string Message
        {
            get
            {
                var now = DateTime.UtcNow;
                return $"CircuitBreaker Error: {_circuitBreakerName} has been tripped for {now.Subtract(_circuitBreakerTripped ?? now).TotalSeconds} seconds.";
            }
        }

        public CircuitBreakerException(CircuitBreakerBase circuitBreaker)
            : this(circuitBreaker.Name, circuitBreaker.Tripped)
        { }
        public CircuitBreakerException(string circuitBreakerName, DateTime? circuitBreakerTripped)
        {
            _circuitBreakerName = circuitBreakerName;
            _circuitBreakerTripped = circuitBreakerTripped;
        }

        private readonly string _circuitBreakerName;
        private readonly DateTime? _circuitBreakerTripped;
    }
}
