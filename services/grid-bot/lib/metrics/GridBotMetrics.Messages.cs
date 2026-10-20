namespace Grid.Bot.Metrics;

using Prometheus;

/// <summary>
/// Defines the Prometheus metrics used by the Grid.Bot service.
/// </summary>
public static partial class GridBotMetrics
{
    /// <summary>
    /// Metrics for Discord messages.
    /// </summary>
    public static class Messages
    {
        /// <summary>
        /// Counts messages processed by the bot, whether or not they were commands.
        /// </summary>
        public static readonly Counter Processed = Prometheus.Metrics.CreateCounter(
            "gridbot_messages_processed_total",
            "The total number of messages processed."
        );
    }
}
