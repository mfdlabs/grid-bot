using MFDLabs.Text.Extensions;
using System;
using System.Threading;

namespace MFDLabs.Sentinels
{
    public class ThresholdExecutionCircuitBreaker : ExecutionCircuitBreakerBase
    {
        protected internal override string Name { get; }
        protected override TimeSpan RetryInterval => _RetryIntervalCalculator();
        protected override DateTime Now => _UtcNowGetter();

        public ThresholdExecutionCircuitBreaker(string name, Func<Exception, bool> failureDetector, Func<TimeSpan> retryIntervalCalculator, Func<int> exceptionCountForTripping, Func<TimeSpan> exceptionIntervalForTripping) 
            : this(name, failureDetector, retryIntervalCalculator, exceptionCountForTripping, exceptionIntervalForTripping, () => DateTime.UtcNow)
        { }
        internal ThresholdExecutionCircuitBreaker(string name, Func<Exception, bool> failureDetector, Func<TimeSpan> retryIntervalCalculator, Func<int> exceptionCountForTripping, Func<TimeSpan> exceptionIntervalForTripping, Func<DateTime> utcNowGetter)
        {
            if (name.IsNullOrWhiteSpace()) throw new ArgumentException("Cannot be null, empty or whitespace", nameof(name));
            Name = name;
            _FailureDetector = failureDetector ?? throw new ArgumentNullException(nameof(failureDetector));
            _RetryIntervalCalculator = retryIntervalCalculator ?? throw new ArgumentNullException(nameof(retryIntervalCalculator));
            _ExceptionCountForTripping = exceptionCountForTripping ?? throw new ArgumentNullException(nameof(exceptionCountForTripping));
            _ExceptionIntervalForTripping = exceptionIntervalForTripping ?? throw new ArgumentNullException(nameof(exceptionIntervalForTripping));
            _UtcNowGetter = utcNowGetter ?? throw new ArgumentNullException(nameof(utcNowGetter));
        }

        private void ResetExceptionCount()
        {
            Interlocked.Exchange(ref _ExceptionCount, 0);
            _ExceptionCountIntervalEnd = Now.Add(_ExceptionIntervalForTripping());
        }
        protected override bool ShouldTrip(Exception ex)
        {
            if (ex == null) throw new ArgumentNullException(nameof(ex));
            if (_FailureDetector(ex))
            {
                if (_ExceptionCountIntervalEnd < Now) ResetExceptionCount();
                Interlocked.Increment(ref _ExceptionCount);
                if (_ExceptionCount > _ExceptionCountForTripping()) return true;
            }
            return false;
        }

        private readonly Func<Exception, bool> _FailureDetector;
        private readonly Func<TimeSpan> _RetryIntervalCalculator;
        private readonly Func<int> _ExceptionCountForTripping;
        private readonly Func<TimeSpan> _ExceptionIntervalForTripping;
        private readonly Func<DateTime> _UtcNowGetter;
        private DateTime _ExceptionCountIntervalEnd = DateTime.MinValue;
        private int _ExceptionCount;
    }
}
