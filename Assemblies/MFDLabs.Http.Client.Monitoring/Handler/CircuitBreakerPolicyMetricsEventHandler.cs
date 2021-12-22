using System;
using System.Collections.Concurrent;
using MFDLabs.Instrumentation;
using MFDLabs.Pipeline;
using MFDLabs.Sentinels.CircuitBreakerPolicy;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Http.Client.Monitoring
{
    public sealed class CircuitBreakerPolicyMetricsEventHandler
    {
        public CircuitBreakerPolicyMetricsEventHandler(ICounterRegistry counterRegistry)
            => _counterRegistry = counterRegistry ?? throw new ArgumentNullException(nameof(counterRegistry));

        public void RegisterEvents(ICircuitBreakerPolicy<IExecutionContext<IHttpRequest, IHttpResponse>> circuitBreakerPolicy, string monitorCategory, string instanceIdentifier)
        {
            if (circuitBreakerPolicy == null)
                throw new ArgumentNullException(nameof(circuitBreakerPolicy));
            if (monitorCategory.IsNullOrWhiteSpace()) 
                throw new ArgumentException("Value has to be a non-empty string.", nameof(monitorCategory));
            if (instanceIdentifier.IsNullOrWhiteSpace()) 
                throw new ArgumentException("Value has to be a non-empty string.", nameof(instanceIdentifier));

            var globalCircuitBreakerMonitor = GetOrCreate(_counterRegistry, GlobalCircuitBreakerInstanceName, instanceIdentifier);
            var instanceCircuitBreakerMonitor = GetOrCreate(_counterRegistry, monitorCategory, instanceIdentifier);
            circuitBreakerPolicy.RequestIntendingToOpenCircuitBreaker += () =>
            {
                instanceCircuitBreakerMonitor.IncrementRequestsThatTripCircuitBreakerPerSecond();
                globalCircuitBreakerMonitor.IncrementRequestsThatTripCircuitBreakerPerSecond();
            };
            circuitBreakerPolicy.CircuitBreakerTerminatingRequest += () =>
            {
                instanceCircuitBreakerMonitor.IncrementRequestsTrippedByCircuitBreakerPerSecond();
                globalCircuitBreakerMonitor.IncrementRequestsTrippedByCircuitBreakerPerSecond();
            };
        }
        private static ICircuitBreakerPolicyPerformanceMonitor GetOrCreate(ICounterRegistry counterRegistry, string monitorCategory, string instanceIdentifier) 
            => Monitors.GetOrAdd(
                $"{monitorCategory}.{instanceIdentifier}",
                _ => new Lazy<ICircuitBreakerPolicyPerformanceMonitor>(() =>
                    new CircuitBreakerPolicyPerInstancePerformanceMonitor(counterRegistry,
                        monitorCategory,
                        instanceIdentifier))
            ).Value;

        private const string GlobalCircuitBreakerInstanceName = "_global_";
        private static readonly ConcurrentDictionary<string, Lazy<ICircuitBreakerPolicyPerformanceMonitor>> Monitors = new();
        private readonly ICounterRegistry _counterRegistry;
    }
}
