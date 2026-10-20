namespace Grid.Bot.Metrics;

using Prometheus;

/// <summary>
/// Defines the Prometheus metrics used by the Grid.Bot service.
/// </summary>
public static partial class GridBotMetrics
{
    /// <summary>
    /// Metrics for processing Discord messages and text commands.
    /// </summary>
    public static class Messages
    {
        /// <summary>
        /// The total number of messages processed.
        /// </summary>
        public static readonly Counter Processed = Prometheus.Metrics.CreateCounter(
            "bot_messages_processed_total",
            "The total number of messages processed."
        );
        /// <summary>
        /// The total number of commands processed.
        /// </summary>
        public static readonly Counter CommandsProcessed = Prometheus.Metrics.CreateCounter(
            "bot_commands_processed_total",
            "The total number of commands processed.",
            "command_name"
        );
        /// <summary>
        /// The total number of messages failed due to maintenance.
        /// </summary>
        public static readonly Counter FailedDueToMaintenance = Prometheus.Metrics.CreateCounter(
            "bot_messages_failed_due_to_maintenance_total",
            "The total number of messages failed due to maintenance."
        );
        /// <summary>
        /// The total number of messages attempted by blacklisted users.
        /// </summary>
        public static readonly Counter BlacklistedUserAttempts = Prometheus.Metrics.CreateCounter(
            "bot_blacklisted_user_attempted_messages_total",
            "The total number of messages attempted by blacklisted users.",
            "message_user_id",
            "message_channel_id",
            "message_guild_id"
        );
        /// <summary>
        /// The total number of users that bypassed maintenance.
        /// </summary>
        public static readonly Counter UsersBypassingMaintenance = Prometheus.Metrics.CreateCounter(
            "bot_messages_users_bypassed_maintenance_total",
            "The total number of users that bypassed maintenance.",
            "message_user_id",
            "message_channel_id",
            "message_guild_id"
        );
        /// <summary>
        /// The time it takes to process an command.
        /// </summary>
        public static readonly Histogram CommandProcessingTime = Prometheus.Metrics.CreateHistogram(
            "bot_command_processing_time_seconds",
            "The time it takes to process an command.",
            "command_name"
        );
    }
}
