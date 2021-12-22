using Discord;
using Discord.WebSocket;

namespace MFDLabs.Grid.Bot.Extensions
{
    internal static class SocketGuildExtensions
    {
        public static SocketApplicationCommand CreateApplicationCommand(this SocketGuild client,
            ApplicationCommandProperties properties,
            RequestOptions options = null)
            => client.CreateApplicationCommandAsync(properties, options).GetAwaiter().GetResult();

    }
}
