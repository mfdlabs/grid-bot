namespace Grid.Bot.Metrics;

using Prometheus;

/// <summary>
/// Defines the Prometheus metrics used by the Grid.Bot service.
/// </summary>
public static partial class GridBotMetrics
{
    /// <summary>
    /// Metrics for alerts sent through Discord webhooks.
    /// </summary>
    public static class DiscordAlerts
    {
        /// <summary>
        /// Counts alerts sent to Discord.
        /// </summary>
        public static readonly Counter Sent = Prometheus.Metrics.CreateCounter(
            "gridbot_discord_webhook_alerts_total",
            "The total number of alerts sent to Discord."
        );

        /// <summary>
        /// Counts alerts sent to Discord with attachments. A subset of <see cref="Sent"/>.
        /// </summary>
        public static readonly Counter SentWithAttachments = Prometheus.Metrics.CreateCounter(
            "gridbot_discord_webhook_alerts_with_attachments_total",
            "The total number of alerts sent to Discord with attachments."
        );
    }
}
