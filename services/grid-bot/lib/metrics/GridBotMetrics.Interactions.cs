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

        /// <summary>
        /// The total number of interactions processed.
        /// </summary>
        public static readonly Counter Processed = Prometheus.Metrics.CreateCounter(
            "bot_interactions_processed_total",
            "The total number of interactions processed.",
            "interaction_type",
            "command_name"
        );
        /// <summary>
        /// The total number of interactions failed due to maintenance.
        /// </summary>
        public static readonly Counter FailedDueToMaintenance = Prometheus.Metrics.CreateCounter(
            "bot_interactions_failed_due_to_maintenance_total",
            "The total number of interactions failed due to maintenance."
        );
        /// <summary>
        /// The total number of interactions attempted by blacklisted users.
        /// </summary>
        public static readonly Counter BlacklistedUserAttempts = Prometheus.Metrics.CreateCounter(
            "bot_blacklisted_user_attempted_interactions_total",
            "The total number of interactions attempted by blacklisted users.",
            "interaction_user_id"
        );
        /// <summary>
        /// The total number of users that bypassed maintenance.
        /// </summary>
        public static readonly Counter UsersBypassingMaintenance = Prometheus.Metrics.CreateCounter(
            "bot_interactions_users_bypassed_maintenance_total",
            "The total number of users that bypassed maintenance.",
            "interaction_user_id"
        );
        /// <summary>
        /// The time it takes to process an interaction.
        /// </summary>
        public static readonly Histogram ProcessingTime = Prometheus.Metrics.CreateHistogram(
            "bot_interaction_processing_time_seconds",
            "The time it takes to process an interaction.",
            "interaction_type",
            "command_name"
        );
        /// <summary>
        /// The total number of interactions failed.
        /// </summary>
        public static readonly Counter Failed = Prometheus.Metrics.CreateCounter(
            "bot_interactions_failed_total",
            "The total number of interactions failed.",
            "interaction_type",
            "command_name"
        );
    }
}
