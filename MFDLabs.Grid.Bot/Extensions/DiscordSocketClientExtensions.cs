using Discord;
using Discord.WebSocket;

namespace MFDLabs.Grid.Bot.Extensions
{
    internal static class DiscordSocketClientExtensions
    {
        public static SocketApplicationCommand CreateGlobalApplicationCommand(this DiscordSocketClient client, ApplicationCommandProperties properties, RequestOptions options = null) 
            => client.CreateGlobalApplicationCommandAsync(properties, options).GetAwaiter().GetResult();
    }
}
