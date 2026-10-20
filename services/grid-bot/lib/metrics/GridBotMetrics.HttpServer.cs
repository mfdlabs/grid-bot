namespace Grid.Bot.Metrics;

using Prometheus;

/// <summary>
/// Defines the Prometheus metrics used by the Grid.Bot service.
/// </summary>
public static partial class GridBotMetrics
{
    /// <summary>
    /// Metrics for requests handled by the HTTP server.
    /// </summary>
    public static class HttpServer
    {
        /// <summary>
        /// The number of concurrent requests being processed by the server.
        /// </summary>
        public static readonly IGauge ConcurrentRequests = Prometheus.Metrics.CreateGauge(
            "http_server_concurrent_requests_total",
            "The number of concurrent requests being processed by the server."
        );
        /// <summary>
        /// Total number of http requests
        /// </summary>
        public static readonly Counter Requests = Prometheus.Metrics.CreateCounter(
            "http_server_requests_total",
            "Total number of http requests",
            "method",
            "endpoint"
        );
        /// <summary>
        /// Total number of http responses
        /// </summary>
        public static readonly Counter Responses = Prometheus.Metrics.CreateCounter(
            "http_server_response_total",
            "Total number of http responses",
            "method",
            "endpoint",
            "status_code"
        );
        /// <summary>
        /// Duration in seconds each request takes
        /// </summary>
        public static readonly Histogram RequestDuration = Prometheus.Metrics.CreateHistogram(
            "http_server_request_duration_seconds",
            "Duration in seconds each request takes",
            "method",
            "endpoint"
        );
    }
}
