using MFDLabs.Text.Extensions;
using System;

namespace MFDLabs.Sentinels
{
    public class ExecutionCircuitBreaker : ExecutionCircuitBreakerBase
    {
        protected internal override string Name { get; }
        protected override TimeSpan RetryInterval => _RetryIntervalCalculator();

        public ExecutionCircuitBreaker(string name, Func<Exception, bool> failureDetector, Func<TimeSpan> retryIntervalCalculator)
        {
            if (name.IsNullOrWhiteSpace()) throw new ArgumentException("Cannot be null, empty or whitespace", nameof(name));
            _FailureDetector = failureDetector ?? throw new ArgumentNullException(nameof(failureDetector));
            _RetryIntervalCalculator = retryIntervalCalculator ?? throw new ArgumentNullException(nameof(retryIntervalCalculator));
            Name = name;
        }

        protected override bool ShouldTrip(Exception ex) => _FailureDetector(ex);

        private readonly Func<Exception, bool> _FailureDetector;
        private readonly Func<TimeSpan> _RetryIntervalCalculator;
    }
}
