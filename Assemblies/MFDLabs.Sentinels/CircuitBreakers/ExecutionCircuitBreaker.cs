using MFDLabs.Text.Extensions;
using System;

namespace MFDLabs.Sentinels
{
    public class ExecutionCircuitBreaker : ExecutionCircuitBreakerBase
    {
        protected internal override string Name { get; }

        protected override TimeSpan RetryInterval
        {
            get
            {
                return _RetryIntervalCalculator();
            }
        }

        public ExecutionCircuitBreaker(string name, Func<Exception, bool> failureDetector, Func<TimeSpan> retryIntervalCalculator)
        {
            if (name.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("Cannot be null, empty or whitespace", "name");
            }

            _FailureDetector = failureDetector ?? throw new ArgumentNullException("failureDetector");
            _RetryIntervalCalculator = retryIntervalCalculator ?? throw new ArgumentNullException("retryIntervalCalculator");
            Name = name;
        }

        protected override bool ShouldTrip(Exception ex)
        {
            return _FailureDetector(ex);
        }

        private readonly Func<Exception, bool> _FailureDetector;

        private readonly Func<TimeSpan> _RetryIntervalCalculator;
    }
}
