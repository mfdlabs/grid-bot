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
        /// The total number of commands failed.
        /// </summary>
        public static readonly Counter Failed = Prometheus.Metrics.CreateCounter(
            "bot_commands_failed_total",
            "The total number of commands failed.",
            "message_type",
            "command"
        );
    }
}
