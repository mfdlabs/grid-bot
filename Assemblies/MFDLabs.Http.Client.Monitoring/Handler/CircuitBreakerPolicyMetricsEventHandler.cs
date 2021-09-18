using MFDLabs.Instrumentation;
using MFDLabs.Pipeline;
using MFDLabs.Sentinels.CircuitBreakerPolicy;
using MFDLabs.Text.Extensions;
using System;
using System.Collections.Concurrent;

namespace MFDLabs.Http.Client.Monitoring
{
    public sealed class CircuitBreakerPolicyMetricsEventHandler
    {
        public CircuitBreakerPolicyMetricsEventHandler(ICounterRegistry counterRegistry)
        {
            _CounterRegistry = counterRegistry ?? throw new ArgumentNullException("counterRegistry");
        }

        public void RegisterEvents(ICircuitBreakerPolicy<IExecutionContext<IHttpRequest, IHttpResponse>> circuitBreakerPolicy, string monitorCategory, string instanceIdentifier)
        {
            if (circuitBreakerPolicy == null)
            {
                throw new ArgumentNullException("circuitBreakerPolicy");
            }
            if (monitorCategory.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("Value has to be a non-empty string.", "monitorCategory");
            }
            if (instanceIdentifier.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("Value has to be a non-empty string.", "instanceIdentifier");
            }
            var globalCircuitBreakerMonitor = GetOrCreate(_CounterRegistry, _GlobalCircuitBreakerInstanceName, instanceIdentifier);
            var instanceCircuitBreakerMonitor = GetOrCreate(_CounterRegistry, monitorCategory, instanceIdentifier);
            circuitBreakerPolicy.RequestIntendingToOpenCircuitBreaker += delegate ()
            {
                instanceCircuitBreakerMonitor.IncrementRequestsThatTripCircuitBreakerPerSecond();
                globalCircuitBreakerMonitor.IncrementRequestsThatTripCircuitBreakerPerSecond();
            };
            circuitBreakerPolicy.CircuitBreakerTerminatingRequest += delegate ()
            {
                instanceCircuitBreakerMonitor.IncrementRequestsTrippedByCircuitBreakerPerSecond();
                globalCircuitBreakerMonitor.IncrementRequestsTrippedByCircuitBreakerPerSecond();
            };
        }

        private static ICircuitBreakerPolicyPerformanceMonitor GetOrCreate(ICounterRegistry counterRegistry, string monitorCategory, string instanceIdentifier)
        {
            return _Monitors.GetOrAdd($"{monitorCategory}.{instanceIdentifier}", (x) =>
            {
                return new Lazy<ICircuitBreakerPolicyPerformanceMonitor>(() => new CircuitBreakerPolicyPerInstancePerformanceMonitor(counterRegistry, monitorCategory, instanceIdentifier));
            }).Value;
        }

        private const string _GlobalCircuitBreakerInstanceName = "_global_";

        private static readonly ConcurrentDictionary<string, Lazy<ICircuitBreakerPolicyPerformanceMonitor>> _Monitors = new ConcurrentDictionary<string, Lazy<ICircuitBreakerPolicyPerformanceMonitor>>();

        private readonly ICounterRegistry _CounterRegistry;
    }
}
