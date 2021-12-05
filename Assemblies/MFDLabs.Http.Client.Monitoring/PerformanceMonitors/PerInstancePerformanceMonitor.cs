using System;
using MFDLabs.Instrumentation;

namespace MFDLabs.Http.Client.Monitoring
{
    internal sealed class PerInstancePerformanceMonitor
    {
        public PerInstancePerformanceMonitor(ICounterRegistry registry, string categoryName, string instanceName)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            AverageResponseTime = registry.GetAverageValueCounter(categoryName, _AverageResponseTimeCounterName, instanceName);
            FailuresPerSecond = registry.GetRateOfCountsPerSecondCounter(categoryName, _FailuresPerSecondCounterName, instanceName);
            RequestsOutstanding = registry.GetRawValueCounter(categoryName, _RequestsOutstandingCounterName, instanceName);
            SuccessesPerSecond = registry.GetRateOfCountsPerSecondCounter(categoryName, _SuccessesPerSecondCounterName, instanceName);
        }

        public IAverageValueCounter AverageResponseTime { get; }
        public IRateOfCountsPerSecondCounter FailuresPerSecond { get; }
        public IRawValueCounter RequestsOutstanding { get; }
        public IRateOfCountsPerSecondCounter SuccessesPerSecond { get; }

        private const string _AverageResponseTimeCounterName = "Avg Response Time";
        private const string _FailuresPerSecondCounterName = "Failures/s";
        private const string _RequestsOutstandingCounterName = "Requests Outstanding";
        private const string _SuccessesPerSecondCounterName = "Requests/s";
    }
}
