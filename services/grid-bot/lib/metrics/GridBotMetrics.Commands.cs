namespace Grid.Bot.Metrics;

using Prometheus;

/// <summary>
/// Defines the Prometheus metrics used by the Grid.Bot service.
/// </summary>
public static partial class GridBotMetrics
{
    /// <summary>
    /// Metrics for text command execution.
    /// </summary>
    public static class Commands
    {
        /// <summary>
        /// Counts text command executions, labeled by command name and outcome.
        /// </summary>
        public static readonly Counter Executions = Prometheus.Metrics.CreateCounter(
            "gridbot_command_executions_total",
            "The total number of text command executions by command and outcome.",
            "command_name",
            "outcome"
        );

        /// <summary>
        /// Measures text command duration in seconds, labeled by command name and outcome.
        /// </summary>
        public static readonly Histogram Duration = Prometheus.Metrics.CreateHistogram(
            "gridbot_command_duration_seconds",
            "The time it takes to execute a text command, labeled by command and outcome.",
            new HistogramConfiguration
            {
                LabelNames = ["command_name", "outcome"],
                Buckets = MetricBuckets.Command,
            }
        );

        /// <summary>
        /// Tracks the number of text commands currently executing.
        /// </summary>
        public static readonly Gauge InFlight = Prometheus.Metrics.CreateGauge(
            "gridbot_commands_in_flight",
            "The number of text commands currently executing."
        );
    }
}
