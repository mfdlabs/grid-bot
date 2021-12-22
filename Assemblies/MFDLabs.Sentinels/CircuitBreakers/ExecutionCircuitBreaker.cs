using MFDLabs.Text.Extensions;
using System;

namespace MFDLabs.Sentinels
{
    public class ExecutionCircuitBreaker : ExecutionCircuitBreakerBase
    {
        protected internal override string Name { get; }
        protected override TimeSpan RetryInterval => _retryIntervalCalculator();

        public ExecutionCircuitBreaker(string name, Func<Exception, bool> failureDetector, Func<TimeSpan> retryIntervalCalculator)
        {
            if (name.IsNullOrWhiteSpace()) throw new ArgumentException("Cannot be null, empty or whitespace", nameof(name));
            _failureDetector = failureDetector ?? throw new ArgumentNullException(nameof(failureDetector));
            _retryIntervalCalculator = retryIntervalCalculator ?? throw new ArgumentNullException(nameof(retryIntervalCalculator));
            Name = name;
        }

        protected override bool ShouldTrip(Exception ex) => _failureDetector(ex);

        private readonly Func<Exception, bool> _failureDetector;
        private readonly Func<TimeSpan> _retryIntervalCalculator;
    }
}
