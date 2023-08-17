#if WE_LOVE_EM_SLASH_COMMANDS

using Discord;
using Discord.WebSocket;
using Threading.Extensions;

namespace Grid.Bot.Extensions
{
    public static class SocketGuildExtensions
    {
        public static SocketApplicationCommand CreateApplicationCommand(
            this SocketGuild client,
            ApplicationCommandProperties properties,
            RequestOptions options = null
        )
            => client.CreateApplicationCommandAsync(properties, options).Sync();

    }
}

#endif