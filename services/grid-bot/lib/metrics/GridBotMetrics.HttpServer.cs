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
    /// <remarks>
    /// The <c>endpoint</c> label is the route pattern, such as <c>/v1/avatar-fetch</c>, or <c>unknown</c>
    /// for requests that did not match a route. It is never the raw request path.
    /// </remarks>
    public static class HttpServer
    {
        /// <summary>
        /// Counts HTTP requests, labeled by method and endpoint.
        /// </summary>
        public static readonly Counter Requests = Prometheus.Metrics.CreateCounter(
            "gridbot_http_server_requests_total",
            "The total number of HTTP requests by method and endpoint.",
            "method",
            "endpoint"
        );

        /// <summary>
        /// Counts HTTP responses, labeled by method, endpoint, and status code.
        /// </summary>
        public static readonly Counter Responses = Prometheus.Metrics.CreateCounter(
            "gridbot_http_server_responses_total",
            "The total number of HTTP responses by method, endpoint, and status code.",
            "method",
            "endpoint",
            "status_code"
        );

        /// <summary>
        /// Measures successful HTTP request duration in seconds, labeled by method and endpoint.
        /// </summary>
        public static readonly Histogram RequestDuration = Prometheus.Metrics.CreateHistogram(
            "gridbot_http_server_request_duration_seconds",
            "The time it takes to handle a successful HTTP request by method and endpoint.",
            new HistogramConfiguration
            {
                LabelNames = ["method", "endpoint"],
                Buckets = MetricBuckets.Fast,
            }
        );

        /// <summary>
        /// Tracks the number of HTTP requests currently being processed.
        /// </summary>
        public static readonly IGauge InFlight = Prometheus.Metrics.CreateGauge(
            "gridbot_http_server_requests_in_flight",
            "The number of HTTP requests currently being processed."
        );
    }
}
