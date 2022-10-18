using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Logging;

namespace MFDLabs.Grid.Bot.Global
{
    public static class BotGlobal
    {
#if DISCORD_SHARDING_ENABLED
        public static DiscordShardedClient Client { get; private set; }

        public static void Initialize(DiscordShardedClient client) => Client = client;
#else
        public static DiscordSocketClient Client { get; private set; }

        public static void Initialize(DiscordSocketClient client) => Client = client;
#endif

        public static async Task SingletonLaunch()
        {
            await Client.LoginAsync(TokenType.Bot, global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotToken).ConfigureAwait(false);
            await Client.StartAsync().ConfigureAwait(false);
        }

        public static async Task TryLogout()
        {
            Logger.Singleton.Log("Attempting to logout bot user...");

            if (Client != null)
            {
                try
                {
                    await Client.StopAsync().ConfigureAwait(false);
                    //await _client.LogoutAsync().ConfigureAwait(false);
                    Logger.Singleton.Info("Bot successfully logged out and stopped!");
                }
                catch
                {
                    Logger.Singleton.Warning("Failed to log out bot user, most likely wasn't logged in prior.");
                }
            }
        }
    }
}
