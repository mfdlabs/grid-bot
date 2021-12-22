using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Logging;

namespace MFDLabs.Grid.Bot.Global
{
    public static class BotGlobal
    {
        public static DiscordSocketClient Client { get; private set; }

        internal static void Initialize(DiscordSocketClient client) => Client = client;

        internal static async Task SingletonLaunch()
        {
            await Client.LoginAsync(TokenType.Bot, global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotToken).ConfigureAwait(false);
            await Client.StartAsync().ConfigureAwait(false);
        }

        internal static async Task TryLogout()
        {
            SystemLogger.Singleton.Log("Attempting to logout bot user...");

            if (Client != null)
            {
                try
                {
                    //await _client.LogoutAsync().ConfigureAwait(false);
                    await Client.StopAsync().ConfigureAwait(false);
                    SystemLogger.Singleton.Info("Bot successfully logged out and stopped!");
                }
                catch
                {
                    SystemLogger.Singleton.Warning("Failed to log out bot user, most likely wasn't logged in prior.");
                }
            }
        }
    }
}
