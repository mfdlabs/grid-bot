using MFDLabs.Text.Extensions;
using System;
using System.Threading;

namespace MFDLabs.Sentinels
{
    public class ThresholdExecutionCircuitBreaker : ExecutionCircuitBreakerBase
    {
        protected internal override string Name { get; }
        protected override TimeSpan RetryInterval => _retryIntervalCalculator();
        protected override DateTime Now => _utcNowGetter();

        public ThresholdExecutionCircuitBreaker(string name, Func<Exception, bool> failureDetector, Func<TimeSpan> retryIntervalCalculator, Func<int> exceptionCountForTripping, Func<TimeSpan> exceptionIntervalForTripping) 
            : this(name, failureDetector, retryIntervalCalculator, exceptionCountForTripping, exceptionIntervalForTripping, () => DateTime.UtcNow)
        { }
        internal ThresholdExecutionCircuitBreaker(string name, Func<Exception, bool> failureDetector, Func<TimeSpan> retryIntervalCalculator, Func<int> exceptionCountForTripping, Func<TimeSpan> exceptionIntervalForTripping, Func<DateTime> utcNowGetter)
        {
            if (name.IsNullOrWhiteSpace()) throw new ArgumentException("Cannot be null, empty or whitespace", nameof(name));
            Name = name;
            _failureDetector = failureDetector ?? throw new ArgumentNullException(nameof(failureDetector));
            _retryIntervalCalculator = retryIntervalCalculator ?? throw new ArgumentNullException(nameof(retryIntervalCalculator));
            _exceptionCountForTripping = exceptionCountForTripping ?? throw new ArgumentNullException(nameof(exceptionCountForTripping));
            _exceptionIntervalForTripping = exceptionIntervalForTripping ?? throw new ArgumentNullException(nameof(exceptionIntervalForTripping));
            _utcNowGetter = utcNowGetter ?? throw new ArgumentNullException(nameof(utcNowGetter));
        }

        private void ResetExceptionCount()
        {
            Interlocked.Exchange(ref _exceptionCount, 0);
            _exceptionCountIntervalEnd = Now.Add(_exceptionIntervalForTripping());
        }
        protected override bool ShouldTrip(Exception ex)
        {
            if (ex == null) throw new ArgumentNullException(nameof(ex));
            if (_failureDetector(ex))
            {
                if (_exceptionCountIntervalEnd < Now) ResetExceptionCount();
                Interlocked.Increment(ref _exceptionCount);
                if (_exceptionCount > _exceptionCountForTripping()) return true;
            }
            return false;
        }

        private readonly Func<Exception, bool> _failureDetector;
        private readonly Func<TimeSpan> _retryIntervalCalculator;
        private readonly Func<int> _exceptionCountForTripping;
        private readonly Func<TimeSpan> _exceptionIntervalForTripping;
        private readonly Func<DateTime> _utcNowGetter;
        private DateTime _exceptionCountIntervalEnd = DateTime.MinValue;
        private int _exceptionCount;
    }
}
