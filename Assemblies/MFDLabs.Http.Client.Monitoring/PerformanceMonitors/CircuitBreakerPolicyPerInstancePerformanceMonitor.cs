﻿using MFDLabs.Instrumentation;
using System;

namespace MFDLabs.Http.Client.Monitoring
{
    internal sealed class CircuitBreakerPolicyPerInstancePerformanceMonitor : ICircuitBreakerPolicyPerformanceMonitor
    {
        private IRateOfCountsPerSecondCounter RequestsTrippedByCircuitBreakerPerSecond { get; set; }

        private IRateOfCountsPerSecondCounter RequestsThatTripCircuitBreakerPerSecond { get; set; }

        public CircuitBreakerPolicyPerInstancePerformanceMonitor(ICounterRegistry counterRegistry, string categoryName, string instanceName)
        {
            _CounterRegistry = counterRegistry ?? throw new ArgumentNullException("counterRegistry");
            RequestsTrippedByCircuitBreakerPerSecond = _CounterRegistry.GetRateOfCountsPerSecondCounter(categoryName, "RequestsTrippedByCircuitBreakerPerSecond", instanceName);
            RequestsThatTripCircuitBreakerPerSecond = _CounterRegistry.GetRateOfCountsPerSecondCounter(categoryName, "RequestsThatTripCircuitBreakerPerSecond", instanceName);
        }

        public void IncrementRequestsThatTripCircuitBreakerPerSecond()
        {
            RequestsThatTripCircuitBreakerPerSecond.Increment();
        }

        public void IncrementRequestsTrippedByCircuitBreakerPerSecond()
        {
            RequestsTrippedByCircuitBreakerPerSecond.Increment();
        }

        private readonly ICounterRegistry _CounterRegistry;
    }
}
