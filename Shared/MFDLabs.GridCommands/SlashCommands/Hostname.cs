/* Copyright MFDLABS Corporation. All rights reserved. */

#if WE_LOVE_EM_SLASH_COMMANDS

using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Networking;
using MFDLabs.Diagnostics;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;

namespace MFDLabs.Grid.Bot.SlashCommands
{
    internal sealed class Hostname : IStateSpecificSlashCommandHandler
    {
        public string CommandDescription => "Get Machine Host Name";
        public string CommandAlias => "hostname";
        public bool Internal => false;
        public bool IsEnabled { get; set; } = true;
        public ulong? GuildId => null;
        public SlashCommandOptionBuilder[] Options => null;

        public async Task Invoke(SocketSlashCommand command)
        {
            using (await command.DeferEphemeralAsync())
            {
                await command.RespondEphemeralAsync(
                    $"The hostname for this instance is: `{SystemGlobal.GetMachineHost()}`\n" +
                    $"The machine ID for this machine is: `{SystemGlobal.GetMachineId()}`\n" +
                    $"The IP address for this machine is: `{NetworkingGlobal.GetLocalIp()}`\n" +
                    "Please paste this into the `Host Name` field in grid-bot-support templates so that the internal team can easily identify this instance."
                );
            }
        }
    }
}

#endif