using System;
using MFDLabs.Instrumentation;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Http.Client.Monitoring
{
    public sealed class ClientPolicyMonitor
    {
        private IRateOfCountsPerSecondCounter RequestsTrippedByCircuitBreakerPerSecond { get; }

        private IRateOfCountsPerSecondCounter FailedRequestsThatTripCircuitBreakerPerSecond { get; }

        private IRateOfCountsPerSecondCounter RequestsExceedingTimeoutPerSecond { get; }

        public ClientPolicyMonitor(ICounterRegistry counterRegistry, string categoryName, string instanceName)
        {
            if (counterRegistry == null) throw new ArgumentNullException(nameof(counterRegistry));
            if (categoryName.IsNullOrWhiteSpace()) throw new ArgumentException("categoryName can not be null or empty");
            if (instanceName.IsNullOrWhiteSpace()) throw new ArgumentException("instanceName can not be null or empty");

            RequestsTrippedByCircuitBreakerPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(categoryName, "RequestsTrippedByCircuitBreakerPerSecond", instanceName);
            FailedRequestsThatTripCircuitBreakerPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(categoryName, "FailedRequestsThatTripCircuitBreakerPerSecond", instanceName);
            RequestsExceedingTimeoutPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(categoryName, "RequestsExceedingTimeoutPerSecond", instanceName);
        }
    }
}
