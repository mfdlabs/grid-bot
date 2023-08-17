namespace Redis;

using System;

using Instrumentation;

internal class PerformanceMonitor
{
    private readonly ICounterRegistry _CounterRegistry;
    private readonly string _PerformanceCategory;

    public IRateOfCountsPerSecondCounter RequestsPerSecond { get; set; }

    public IRawValueCounter OutstandingRequestCount { get; set; }

    public IAverageValueCounter AverageResponseTime { get; set; }

    public IRateOfCountsPerSecondCounter ErrorsPerSecond { get; set; }

    public PerformanceMonitor(ICounterRegistry counterRegistry, string performanceCategory)
    {
        _CounterRegistry = counterRegistry ?? throw new ArgumentNullException(nameof(counterRegistry));
        _PerformanceCategory = performanceCategory ?? throw new ArgumentNullException(nameof(performanceCategory));

        RequestsPerSecond = _CounterRegistry.GetRateOfCountsPerSecondCounter(performanceCategory, "Requests/s");
        OutstandingRequestCount = _CounterRegistry.GetRawValueCounter(performanceCategory, "Outstanding Request Count");
        AverageResponseTime = _CounterRegistry.GetAverageValueCounter(performanceCategory, "Average Response Time");
        ErrorsPerSecond = _CounterRegistry.GetRateOfCountsPerSecondCounter(performanceCategory, "ErrorsPerSecond");
    }

    public IRateOfCountsPerSecondCounter GetPerEndpointErrorCounter(string endPoint) 
        => _CounterRegistry.GetRateOfCountsPerSecondCounter(_PerformanceCategory, "Endpoint Errors/s", endPoint);
}
