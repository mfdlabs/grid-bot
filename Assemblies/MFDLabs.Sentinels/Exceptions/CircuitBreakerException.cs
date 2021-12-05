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
                return $"CircuitBreaker Error: {_CircuitBreakerName} has been tripped for {now.Subtract(_CircuitBreakerTripped ?? now).TotalSeconds} seconds.";
            }
        }

        public CircuitBreakerException(CircuitBreakerBase circuitBreaker)
            : this(circuitBreaker.Name, circuitBreaker.Tripped)
        { }
        public CircuitBreakerException(string circuitBreakerName, DateTime? circuitBreakerTripped)
        {
            _CircuitBreakerName = circuitBreakerName;
            _CircuitBreakerTripped = circuitBreakerTripped;
        }

        private readonly string _CircuitBreakerName;
        private readonly DateTime? _CircuitBreakerTripped;
    }
}
