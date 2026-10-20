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
        /// Total number of alerts sent to Discord
        /// </summary>
        public static readonly Counter AlertsSent = Prometheus.Metrics.CreateCounter(
            "discord_webhook_alert_total",
            "Total number of alerts sent to Discord"
        );
        /// <summary>
        /// Total number of alerts sent to Discord with attachments
        /// </summary>
        public static readonly Counter AlertsWithAttachmentsSent = Prometheus.Metrics.CreateCounter(
            "discord_webhook_alerts_with_attachments_total",
            "Total number of alerts sent to Discord with attachments"
        );
    }
}
