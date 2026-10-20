namespace Grid.Bot.Metrics;

using Prometheus;

/// <summary>
/// Defines the Prometheus metrics used by the Grid.Bot service.
/// </summary>
public static partial class GridBotMetrics
{
    /// <summary>
    /// Metrics for the bot's gRPC API.
    /// </summary>
    public static class Grpc
    {
        /// <summary>
        /// Total number of gRPC health check requests
        /// </summary>
        public static readonly Counter HealthCheckRequests = Prometheus.Metrics.CreateCounter(
            "grpc_health_check_requests_total",
            "Total number of gRPC health check requests"
        );
        /// <summary>
        /// Total number of gRPC set status requests
        /// </summary>
        public static readonly Counter SetStatusRequests = Prometheus.Metrics.CreateCounter(
            "grpc_set_status_requests_total",
            "Total number of gRPC set status requests"
        );
    }
}
