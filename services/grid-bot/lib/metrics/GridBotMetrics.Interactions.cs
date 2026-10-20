namespace Grid.Bot.Metrics;

using Prometheus;

/// <summary>
/// Defines the Prometheus metrics used by the Grid.Bot service.
/// </summary>
public static partial class GridBotMetrics
{
    /// <summary>
    /// Metrics for processing Discord interactions and interaction commands.
    /// </summary>
    public static class Interactions
    {
        /// <summary>
        /// Counts interaction executions, labeled by interaction type, command name, and outcome.
        /// </summary>
        public static readonly Counter Executions = Prometheus.Metrics.CreateCounter(
            "gridbot_interaction_executions_total",
            "The total number of interaction executions by type, command, and outcome.",
            "interaction_type",
            "command_name",
            "outcome"
        );

        /// <summary>
        /// Measures interaction duration in seconds from dispatch to completion, labeled by interaction type, command name, and outcome.
        /// </summary>
        public static readonly Histogram Duration = Prometheus.Metrics.CreateHistogram(
            "gridbot_interaction_duration_seconds",
            "The time from dispatching an interaction to its completion, labeled by type, command, and outcome.",
            new HistogramConfiguration
            {
                LabelNames = ["interaction_type", "command_name", "outcome"],
                Buckets = MetricBuckets.Command,
            }
        );

        /// <summary>
        /// Tracks the number of interactions currently executing.
        /// </summary>
        public static readonly Gauge InFlight = Prometheus.Metrics.CreateGauge(
            "gridbot_interactions_in_flight",
            "The number of interactions currently executing."
        );
    }
}
