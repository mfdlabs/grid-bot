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
        /// The total number of log messages.
        /// </summary>
        public static readonly Counter Messages = Prometheus.Metrics.CreateCounter(
            "bot_discord_log_messages_total",
            "The total number of log messages.",
            "log_severity"
        );
        /// <summary>
        /// The total number of serializer errors.
        /// </summary>
        public static readonly Counter SerializerErrors = Prometheus.Metrics.CreateCounter(
            "bot_discord_serializer_errors_total",
            "The total number of serializer errors."
        );
    }
}
