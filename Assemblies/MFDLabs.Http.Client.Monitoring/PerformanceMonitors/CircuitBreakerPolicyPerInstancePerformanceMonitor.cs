using System;
using MFDLabs.Instrumentation;

namespace MFDLabs.Http.Client.Monitoring
{
    internal sealed class CircuitBreakerPolicyPerInstancePerformanceMonitor : ICircuitBreakerPolicyPerformanceMonitor
    {
        private IRateOfCountsPerSecondCounter RequestsTrippedByCircuitBreakerPerSecond { get; }
        private IRateOfCountsPerSecondCounter RequestsThatTripCircuitBreakerPerSecond { get; }

        public CircuitBreakerPolicyPerInstancePerformanceMonitor(ICounterRegistry counterRegistry, string categoryName, string instanceName)
        {
            var counterRegistry1 = counterRegistry ?? throw new ArgumentNullException(nameof(counterRegistry));
            RequestsTrippedByCircuitBreakerPerSecond = counterRegistry1.GetRateOfCountsPerSecondCounter(categoryName, "RequestsTrippedByCircuitBreakerPerSecond", instanceName);
            RequestsThatTripCircuitBreakerPerSecond = counterRegistry1.GetRateOfCountsPerSecondCounter(categoryName, "RequestsThatTripCircuitBreakerPerSecond", instanceName);
        }

        public void IncrementRequestsThatTripCircuitBreakerPerSecond()
        {
            RequestsThatTripCircuitBreakerPerSecond.Increment();
        }
        public void IncrementRequestsTrippedByCircuitBreakerPerSecond()
        {
            RequestsTrippedByCircuitBreakerPerSecond.Increment();
        }
    }
}
