namespace Grid.Bot.Metrics;

using Prometheus;

/// <summary>
/// Defines the Prometheus metrics used by the Grid.Bot service.
/// </summary>
public static partial class GridBotMetrics
{
    /// <summary>
    /// Metrics for messages emitted by the Discord client library.
    /// </summary>
    public static class DiscordLogs
    {
        /// <summary>
        /// Counts Discord log messages, labeled by severity.
        /// </summary>
        public static readonly Counter Messages = Prometheus.Metrics.CreateCounter(
            "gridbot_discord_log_messages_total",
            "The total number of Discord log messages by severity.",
            "severity"
        );

        /// <summary>
        /// Counts serializer errors reported by the Discord client library.
        /// </summary>
        public static readonly Counter SerializerErrors = Prometheus.Metrics.CreateCounter(
            "gridbot_discord_serializer_errors_total",
            "The total number of Discord serializer errors."
        );
    }
}
