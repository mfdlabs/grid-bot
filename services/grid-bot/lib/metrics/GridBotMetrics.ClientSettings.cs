namespace Grid.Bot.Metrics;

using Prometheus;

/// <summary>
/// Defines the Prometheus metrics used by the Grid.Bot service.
/// </summary>
public static partial class GridBotMetrics
{
    /// <summary>
    /// Metrics for the client-settings service.
    /// </summary>
    public static class ClientSettings
    {
        /// <summary>
        /// Counts client-settings refreshes, labeled by application.
        /// </summary>
        public static readonly Counter Refreshes = Prometheus.Metrics.CreateCounter(
            "gridbot_client_settings_refreshes_total",
            "The total number of client-settings refreshes by application.",
            "application"
        );

        /// <summary>
        /// Counts client-settings writes, labeled by application.
        /// </summary>
        public static readonly Counter Writes = Prometheus.Metrics.CreateCounter(
            "gridbot_client_settings_writes_total",
            "The total number of client-settings writes by application.",
            "application"
        );

        /// <summary>
        /// Counts client-settings reads, labeled by application. Reads of every application are labeled <c>all</c>.
        /// </summary>
        public static readonly Counter Reads = Prometheus.Metrics.CreateCounter(
            "gridbot_client_settings_reads_total",
            "The total number of client-settings reads by application.",
            "application"
        );

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
    }
}
