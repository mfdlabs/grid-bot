using System;
using MFDLabs.Instrumentation;

namespace MFDLabs.Http.Client.Monitoring
{
    internal sealed class PerInstancePerformanceMonitor
    {
        public PerInstancePerformanceMonitor(ICounterRegistry registry, string categoryName, string instanceName)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            AverageResponseTime = registry.GetAverageValueCounter(categoryName, AverageResponseTimeCounterName, instanceName);
            FailuresPerSecond = registry.GetRateOfCountsPerSecondCounter(categoryName, FailuresPerSecondCounterName, instanceName);
            RequestsOutstanding = registry.GetRawValueCounter(categoryName, RequestsOutstandingCounterName, instanceName);
            SuccessesPerSecond = registry.GetRateOfCountsPerSecondCounter(categoryName, SuccessesPerSecondCounterName, instanceName);
        }

        public IAverageValueCounter AverageResponseTime { get; }
        public IRateOfCountsPerSecondCounter FailuresPerSecond { get; }
        public IRawValueCounter RequestsOutstanding { get; }
        public IRateOfCountsPerSecondCounter SuccessesPerSecond { get; }

        private const string AverageResponseTimeCounterName = "Avg Response Time";
        private const string FailuresPerSecondCounterName = "Failures/s";
        private const string RequestsOutstandingCounterName = "Requests Outstanding";
        private const string SuccessesPerSecondCounterName = "Requests/s";
    }
}
