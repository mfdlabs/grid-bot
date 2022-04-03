/* Copyright MFDLABS Corporation. All rights reserved. */

#if WE_LOVE_EM_SLASH_COMMANDS

using Discord;
using Discord.WebSocket;

namespace MFDLabs.Grid.Bot.Extensions
{
    public static class SocketGuildExtensions
    {
        public static SocketApplicationCommand CreateApplicationCommand(
            this SocketGuild client,
            ApplicationCommandProperties properties,
            RequestOptions options = null
        )
            => client.CreateApplicationCommandAsync(properties, options).GetAwaiter().GetResult();

    }
}

#endif