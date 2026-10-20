namespace Grid.Bot.Events;

using System;
using System.Threading.Tasks;

using Discord.WebSocket;

using Grid.Bot.Metrics;

/// <summary>
/// Records Discord gateway metrics for each shard.
/// </summary>
public static class GatewayMetricsHandlers
{
    /// <summary>
    /// Records the latest latency of a shard.
    /// </summary>
    /// <param name="oldLatency">The previous latency in milliseconds.</param>
    /// <param name="newLatency">The new latency in milliseconds.</param>
    /// <param name="shard">The <see cref="DiscordSocketClient"/>.</param>
    /// <returns>A completed <see cref="Task"/>.</returns>
    public static Task OnShardLatencyUpdated(int oldLatency, int newLatency, DiscordSocketClient shard)
    {
        var shardId = shard.ShardId.ToString();

        GridBotMetrics.DiscordGateway.Latency
            .WithLabels(shardId)
            .Set(newLatency / 1000d);

        // Create the shard's disconnect series at zero, so its first disconnect is counted by increase().
        GridBotMetrics.DiscordGateway.Disconnects.WithLabels(shardId);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Counts a shard disconnect.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> that caused the disconnect.</param>
    /// <param name="shard">The <see cref="DiscordSocketClient"/>.</param>
    /// <returns>A completed <see cref="Task"/>.</returns>
    public static Task OnShardDisconnected(Exception exception, DiscordSocketClient shard)
    {
        GridBotMetrics.DiscordGateway.Disconnects.WithLabels(shard.ShardId.ToString()).Inc();

        return Task.CompletedTask;
    }
}
