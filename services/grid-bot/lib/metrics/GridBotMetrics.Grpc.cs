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
        /// Counts gRPC requests, labeled by method (see <see cref="MetricLabelValues.GrpcMethod"/>).
        /// </summary>
        public static readonly Counter Requests = Prometheus.Metrics.CreateCounter(
            "gridbot_grpc_requests_total",
            "The total number of gRPC requests by method.",
            "method"
        );
    }
}
