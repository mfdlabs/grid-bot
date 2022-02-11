using System.Threading.Tasks;
using Discord;
using MFDLabs.Grid.Bot.Global;
using MFDLabs.Grid.Bot.Registries;
using MFDLabs.Logging;
using MFDLabs.Text.Extensions;

#if DISCORD_SHARDING_ENABLED
using Discord.WebSocket;
#endif

namespace MFDLabs.Grid.Bot.Events
{
#if DISCORD_SHARDING_ENABLED
    public static class OnShardReady
    {
        private static string GetStatusText(string updateText)
        {
            return updateText.IsNullOrEmpty() ? "Maintenance is enabled" : $"Maintenance is enabled: {updateText}";
        }

        public static async Task Invoke(DiscordSocketClient shard)
        {

            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.RegisterCommandRegistryAtAppStart)
                CommandRegistry.RegisterOnce();

            SystemLogger.Singleton.Debug(
                "Shard '{0}' ready as '{0}#{1}'",
                shard.ShardId,
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

            SystemLogger.Singleton.Debug(
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
