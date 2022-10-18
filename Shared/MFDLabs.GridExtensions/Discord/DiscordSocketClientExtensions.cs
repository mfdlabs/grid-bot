/* Copyright MFDLABS Corporation. All rights reserved. */

#if WE_LOVE_EM_SLASH_COMMANDS && !DISCORD_SHARDING_ENABLED

using Discord;
using Discord.WebSocket;
using MFDLabs.Threading.Extensions;

namespace MFDLabs.Grid.Bot.Extensions
{
    public static class DiscordSocketClientExtensions
    {
        public static SocketApplicationCommand CreateGlobalApplicationCommand(this DiscordSocketClient client,
            ApplicationCommandProperties properties,
            RequestOptions options = null) 
            => client.CreateGlobalApplicationCommandAsync(properties, options).Sync();
    }
}

#endif