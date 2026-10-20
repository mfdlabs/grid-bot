namespace Grid.Bot.Metrics;

using Prometheus;

/// <summary>
/// Defines the Prometheus metrics used by the Grid.Bot service.
/// </summary>
public static partial class GridBotMetrics
{
    /// <summary>
    /// Metrics for the Grid server job lifecycle.
    /// </summary>
    public static class GridJobs
    {
        /// <summary>
        /// Measures how long it takes to acquire a Grid job in seconds, labeled by outcome.
        /// </summary>
        public static readonly Histogram AcquireDuration = Prometheus.Metrics.CreateHistogram(
            "gridbot_grid_job_acquire_duration_seconds",
            "The time it takes to acquire a Grid job, including waiting for a ready instance.",
            new HistogramConfiguration
            {
                LabelNames = ["outcome"],
                Buckets = MetricBuckets.Fast,
            }
        );

        /// <summary>
        /// Counts rejected Grid jobs, labeled by rejection reason.
        /// </summary>
        public static readonly Counter Rejections = Prometheus.Metrics.CreateCounter(
            "gridbot_grid_job_rejections_total",
            "The total number of Grid jobs rejected.",
            "reason"
        );

        /// <summary>
        /// Counts closed Grid jobs, labeled by whether recycling the instance was attempted.
        /// </summary>
        public static readonly Counter Closed = Prometheus.Metrics.CreateCounter(
            "gridbot_grid_jobs_closed_total",
            "The total number of Grid jobs closed.",
            "recycle"
        );

        /// <summary>
        /// Reports the number of active Grid jobs.
        /// </summary>
        public static readonly Gauge Active = Prometheus.Metrics.CreateGauge(
            "gridbot_grid_active_jobs",
            "The number of active Grid jobs."
        );

        /// <summary>
        /// Reports the number of Grid server instances.
        /// </summary>
        public static readonly Gauge Instances = Prometheus.Metrics.CreateGauge(
            "gridbot_grid_instances",
            "The number of Grid server instances."
        );

        /// <summary>
        /// Reports the number of Grid server instances ready to accept a job.
        /// </summary>
        public static readonly Gauge ReadyInstances = Prometheus.Metrics.CreateGauge(
            "gridbot_grid_ready_instances",
            "The number of Grid server instances ready to accept a job."
        );
    }
}
