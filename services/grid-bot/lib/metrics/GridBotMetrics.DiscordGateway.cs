namespace Grid.Bot.Metrics;

using Prometheus;

/// <summary>
/// Defines the Prometheus metrics used by the Grid.Bot service.
/// </summary>
public static partial class GridBotMetrics
{
    /// <summary>
    /// Metrics for the Discord gateway connection, labeled by shard.
    /// </summary>
    public static class DiscordGateway
    {
        /// <summary>
        /// Reports the latest gateway latency in seconds, labeled by shard identifier.
        /// </summary>
        public static readonly Gauge Latency = Prometheus.Metrics.CreateGauge(
            "gridbot_discord_gateway_latency_seconds",
            "The latest Discord gateway latency in seconds.",
            "shard_id"
        );

        /// <summary>
        /// Counts gateway disconnects, labeled by shard identifier.
        /// </summary>
        public static readonly Counter Disconnects = Prometheus.Metrics.CreateCounter(
            "gridbot_discord_gateway_disconnects_total",
            "The total number of Discord gateway disconnects.",
            "shard_id"
        );
    }
}
