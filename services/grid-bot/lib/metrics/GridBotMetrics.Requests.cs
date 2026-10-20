namespace Grid.Bot.Metrics;

using Prometheus;

/// <summary>
/// Defines the Prometheus metrics used by the Grid.Bot service.
/// </summary>
public static partial class GridBotMetrics
{
    /// <summary>
    /// Metrics for requests that were refused before they ran.
    /// </summary>
    public static class Requests
    {
        /// <summary>
        /// Counts requests refused before they ran, labeled by source and reason
        /// (see <see cref="MetricLabelValues.Source"/> and <see cref="MetricLabelValues.RejectionReason"/>).
        /// </summary>
        public static readonly Counter Rejected = Prometheus.Metrics.CreateCounter(
            "gridbot_requests_rejected_total",
            "The total number of requests refused before they ran by source and reason.",
            "source",
            "reason"
        );

        /// <summary>
        /// Counts requests from users who bypassed maintenance mode, labeled by source.
        /// </summary>
        public static readonly Counter MaintenanceBypasses = Prometheus.Metrics.CreateCounter(
            "gridbot_maintenance_bypasses_total",
            "The total number of requests that bypassed maintenance mode by source.",
            "source"
        );

        /// <summary>
        /// Counts requests refused by a flood checker, labeled by feature and scope
        /// (see <see cref="MetricLabelValues.Feature"/> and <see cref="MetricLabelValues.FloodScope"/>).
        /// </summary>
        public static readonly Counter FloodCheckRejections = Prometheus.Metrics.CreateCounter(
            "gridbot_flood_check_rejections_total",
            "The total number of requests refused by a flood checker by feature and scope.",
            "feature",
            "scope"
        );
    }
}
