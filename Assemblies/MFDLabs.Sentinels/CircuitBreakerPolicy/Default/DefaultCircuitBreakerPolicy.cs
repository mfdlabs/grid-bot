using System;
using System.Threading;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Sentinels.CircuitBreakerPolicy
{
    public class DefaultCircuitBreakerPolicy<TExecutionContext> : CircuitBreakerPolicyBase<TExecutionContext>
    {
        public DefaultCircuitBreakerPolicy(string circuitBreakerIdentifier, IDefaultCircuitBreakerPolicyConfig circuitBreakerPolicyConfig, ITripReasonAuthority<TExecutionContext> tripReasonAuthority) 
            : base(tripReasonAuthority)
        {
            if (circuitBreakerIdentifier.IsNullOrWhiteSpace()) 
                throw new ArgumentException("Has to be a non-empty string.", nameof(circuitBreakerIdentifier));
            Config = circuitBreakerPolicyConfig ?? throw new ArgumentNullException(nameof(circuitBreakerPolicyConfig));
            if (Config.FailuresAllowedBeforeTrip < 0) 
                throw new ArgumentException("FailuresAllowedBeforeTrip cannot be negative.", nameof(circuitBreakerPolicyConfig));
            _circuitBreaker = new CircuitBreaker(circuitBreakerIdentifier);
        }

        protected override bool IsCircuitBreakerOpened(TExecutionContext executionContext, out CircuitBreakerException exception)
        {
            exception = null;
            if (!_circuitBreaker.IsTripped) return false;
            if (_nextRetry <= DateTime.UtcNow && Interlocked.CompareExchange(ref _shouldRetrySignal, 1, 0) == 0) return false;
            exception = new CircuitBreakerException(_circuitBreaker);
            return true;
        }
        protected override void OnSuccessfulRequest(TExecutionContext executionContext)
        {
            Interlocked.Exchange(ref _consecutiveErrorsCount, 0);
            _circuitBreaker.Reset();
        }
        protected override void OnNotified(TExecutionContext executionContext) => Interlocked.Exchange(ref _shouldRetrySignal, 0);
        protected override bool TryToTripCircuitBreaker(TExecutionContext executionContext)
        {
            Interlocked.Increment(ref _consecutiveErrorsCount);
            if (_consecutiveErrorsCount <= Config.FailuresAllowedBeforeTrip) return false;
            _nextRetry = DateTime.UtcNow.Add(Config.RetryInterval);
            _circuitBreaker.Trip();
            return true;
        }

        private readonly CircuitBreaker _circuitBreaker;
        private int _shouldRetrySignal;
        private DateTime _nextRetry = DateTime.MinValue;
        private int _consecutiveErrorsCount;
        protected readonly IDefaultCircuitBreakerPolicyConfig Config;
    }
}
