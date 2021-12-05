﻿using System;
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
            _CircuitBreaker = new CircuitBreaker(circuitBreakerIdentifier);
        }

        protected override bool IsCircuitBreakerOpened(TExecutionContext executionContext, out CircuitBreakerException exception)
        {
            exception = null;
            if (!_CircuitBreaker.IsTripped) return false;
            if (_NextRetry <= DateTime.UtcNow && Interlocked.CompareExchange(ref _ShouldRetrySignal, 1, 0) == 0) return false;
            exception = new CircuitBreakerException(_CircuitBreaker);
            return true;
        }
        protected override void OnSuccessfulRequest(TExecutionContext executionContext)
        {
            Interlocked.Exchange(ref _ConsecutiveErrorsCount, 0);
            _CircuitBreaker.Reset();
        }
        protected override void OnNotified(TExecutionContext executionContext) => Interlocked.Exchange(ref _ShouldRetrySignal, 0);
        protected override bool TryToTripCircuitBreaker(TExecutionContext executionContext)
        {
            Interlocked.Increment(ref _ConsecutiveErrorsCount);
            if (_ConsecutiveErrorsCount <= Config.FailuresAllowedBeforeTrip) return false;
            _NextRetry = DateTime.UtcNow.Add(Config.RetryInterval);
            _CircuitBreaker.Trip();
            return true;
        }

        private readonly CircuitBreaker _CircuitBreaker;
        private int _ShouldRetrySignal;
        private DateTime _NextRetry = DateTime.MinValue;
        private int _ConsecutiveErrorsCount;
        protected readonly IDefaultCircuitBreakerPolicyConfig Config;
    }
}
