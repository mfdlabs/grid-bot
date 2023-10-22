namespace Redis;

using Prometheus;

internal static class PerformanceMonitor
{
    private static readonly Counter _EndPointErrors = Metrics.CreateCounter("redis_endpoint_errors", "Number of errors per endpoint", "endpoint");

    public static Counter RequestPerSecondCounter { get; } = Metrics.CreateCounter("redis_requests_per_second", "Number of requests per second");
    public static Gauge OutstandingRequestsGauge { get; } = Metrics.CreateGauge("redis_outstanding_requests", "Number of outstanding requests");
    public static Histogram AverageRequestDurationHistogram { get; } = Metrics.CreateHistogram("redis_average_request_duration", "Average request duration in milliseconds");
    public static Counter ErrorsPerSecondCounter { get; } = Metrics.CreateCounter("redis_errors_per_second", "Number of errors per second");

    public static Counter.Child GetPerEndpointErrorCounter(string endPoint) 
        => _EndPointErrors.WithLabels(endPoint);
}
