using System.Threading.Tasks;

using Discord;

using MFDLabs.Logging;
using MFDLabs.Threading;
using MFDLabs.Text.Extensions;
using MFDLabs.Grid.Bot.Global;
using MFDLabs.Grid.Bot.Registries;

#if DISCORD_SHARDING_ENABLED
using Discord.WebSocket;
#endif

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

namespace MFDLabs.Grid.Bot.Events
{
#if DISCORD_SHARDING_ENABLED
    public static class OnShardReady
    {
        private static Atomic<int> _shardCount = 0; // needs to be atomic due to the race situation here.

        private static string GetStatusText(string updateText)
        {
            return updateText.IsNullOrEmpty() ? "Maintenance is enabled" : $"Maintenance is enabled: {updateText}";
        }

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

                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.RegisterCommandRegistryAtAppStart)
                    CommandRegistry.RegisterOnce();

                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.IsEnabled)
                {
                    var text = global::MFDLabs.Grid.Bot.Properties.Settings.Default.ReasonForDying;
                    BotRegistry.Client.SetStatusAsync(UserStatus.DoNotDisturb);
                    BotRegistry.Client.SetGameAsync(GetStatusText(text), null, ActivityType.Playing);
                    return;
                }

                BotRegistry.Client.SetStatusAsync(
                    global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotGlobalUserStatus
                );

                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotGlobalStatusMessage.IsNullOrEmpty())
                    BotRegistry.Client.SetGameAsync(
                        global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotGlobalStatusMessage,
                        global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotGlobalStreamURL,
                        global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotGlobalActivityType
                    );
            }
        }
    }
#else
    public static class OnReady
    {
        private static string GetStatusText(string updateText)
        {
            return updateText.IsNullOrEmpty() ? "Maintenance is enabled" : $"Maintenance is enabled: {updateText}";
        }

        public static async Task Invoke()
        {

            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.RegisterCommandRegistryAtAppStart)
                CommandRegistry.RegisterOnce();

            Logger.Singleton.Debug(
                "Bot ready as '{0}#{1}'",
                BotGlobal.Client.CurrentUser.Username,
                BotGlobal.Client.CurrentUser.Discriminator
            );

            if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.IsEnabled)
            {
                var text = global::MFDLabs.Grid.Bot.Properties.Settings.Default.ReasonForDying;
                await BotGlobal.Client.SetStatusAsync(UserStatus.DoNotDisturb);
                await BotGlobal.Client.SetGameAsync(GetStatusText(text), null, ActivityType.Playing);
                return;
            }

            await BotGlobal.Client.SetStatusAsync(
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotGlobalUserStatus
            );

            if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotGlobalStatusMessage.IsNullOrEmpty())
                await BotGlobal.Client.SetGameAsync(
                    global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotGlobalStatusMessage,
                    global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotGlobalStreamURL,
                    global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotGlobalActivityType
                );
        }
    }
#endif
}

#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
