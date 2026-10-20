namespace Grid.Bot.Metrics;

using Prometheus;

/// <summary>
/// Defines the Prometheus metrics used by the Grid.Bot service.
/// </summary>
public static partial class GridBotMetrics
{
    /// <summary>
    /// Metrics for client-settings reads, writes, and refreshes.
    /// </summary>
    public static class ClientSettings
    {
        /// <summary>
        /// Measures client-settings refresh duration in seconds, labeled by outcome.
        /// </summary>
        public static readonly Histogram RefreshDuration = Prometheus.Metrics.CreateHistogram(
            "gridbot_client_settings_refresh_duration_seconds",
            "The time it takes to refresh the client settings, labeled by outcome.",
            new HistogramConfiguration
            {
                LabelNames = ["outcome"],
                Buckets = MetricBuckets.Fast,
            }
        );

        /// <summary>
        /// Number of times the client settings have been refreshed.
        /// </summary>
        public static readonly Counter Refreshes = Prometheus.Metrics.CreateCounter(
            "rbx_client_settings_refresh_total",
            "Number of times the client settings have been refreshed.",
            "application"
        );
        /// <summary>
        /// Number of times the client settings have been written.
        /// </summary>
        public static readonly Counter Writes = Prometheus.Metrics.CreateCounter(
            "rbx_client_settings_write_total",
            "Number of times the client settings have been written.",
            "application"
        );
        /// <summary>
        /// Number of times the client settings have been read.
        /// </summary>
        public static readonly Counter Reads = Prometheus.Metrics.CreateCounter(
            "rbx_client_settings_read_total",
            "Number of times the client settings have been read.",
            "application"
        );
    }
}
