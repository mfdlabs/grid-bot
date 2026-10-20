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
        /// Total number of exceptions uploaded to Backtrace
        /// </summary>
        public static readonly Counter ExceptionsUploaded = Prometheus.Metrics.CreateCounter(
            "backtrace_exception_upload_total",
            "Total number of exceptions uploaded to Backtrace"
        );
    }
}
