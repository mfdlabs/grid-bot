namespace Grid.Bot.Metrics;

using Prometheus;

/// <summary>
/// Defines the Prometheus metrics used by the Grid.Bot service.
/// </summary>
public static partial class GridBotMetrics
{
    /// <summary>
    /// Metrics for exception uploads to Backtrace.
    /// </summary>
    public static class Backtrace
    {
        /// <summary>
        /// Counts exceptions uploaded to Backtrace.
        /// </summary>
        public static readonly Counter Uploads = Prometheus.Metrics.CreateCounter(
            "gridbot_backtrace_uploads_total",
            "The total number of exceptions uploaded to Backtrace."
        );
    }
}
