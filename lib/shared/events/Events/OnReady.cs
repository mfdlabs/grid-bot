namespace Grid.Bot.Events;

using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using Logging;

using Threading;
using Text.Extensions;

using Global;
using Registries;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

/// <summary>
/// Event handler to be invoked when a shard is ready,
/// </summary>
public static class OnShardReady
{
    private static Atomic<int> _shardCount = 0; // needs to be atomic due to the race situation here.

    private static string GetStatusText(string updateText)
        => updateText.IsNullOrEmpty() ? "Maintenance is enabled" : $"Maintenance is enabled: {updateText}";

    /// <summary>
    /// Invoe the event handler.
    /// </summary>
    /// <param name="shard">The client for the shard.</param>
    public static async Task Invoke(DiscordSocketClient shard)
    {
        _shardCount++;

        Logger.Singleton.Debug(
            "Shard '{0}' ready as '{0}#{1}'",
            shard.ShardId,
            BotRegistry.Client.CurrentUser.Username,
            BotRegistry.Client.CurrentUser.Discriminator
        );

        if (_shardCount == BotRegistry.Client.Shards.Count)
        {
            Logger.Singleton.Debug("Final shard ready!");

            BotRegistry.Ready = true;

            if (CommandsSettings.Singleton.RegisterCommandRegistryAtAppStart)
                CommandRegistry.RegisterOnce();

            if (MaintenanceSettings.Singleton.MaintenanceEnabled)
            {
                var text = MaintenanceSettings.Singleton.MaintenanceStatus;

                BotRegistry.Client.SetStatusAsync(UserStatus.DoNotDisturb);
                BotRegistry.Client.SetGameAsync(GetStatusText(text));

                return;
            }

            BotRegistry.Client.SetStatusAsync(DiscordSettings.Singleton.BotStatus);

            if (!DiscordSettings.Singleton.BotStatusMessage.IsNullOrEmpty())
                BotRegistry.Client.SetGameAsync(
                    DiscordSettings.Singleton.BotStatusMessage
                );
        }
    }
}

#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
