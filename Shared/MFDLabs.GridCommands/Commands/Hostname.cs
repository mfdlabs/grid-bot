/* Copyright MFDLABS Corporation. All rights reserved. */

using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Networking;
using MFDLabs.Diagnostics;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;

/** TODO: Bridge Datacenter information here for distributed app cells? **/

namespace MFDLabs.Grid.Bot.Commands
{
    internal sealed class Hostname : IStateSpecificCommandHandler
    {
        public string CommandName => "Get Machine Hostname";
        public string CommandDescription => "Fetches the hostname of the machine that the bot is running on, this is useful for debugging distributed apps.";
        public string[] CommandAliases => new[] { "host", "hostname", "machine" };
        public bool Internal => false;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            // Reply with the hostname
            await message.ReplyAsync(
                $"The hostname for this instance is: `{SystemGlobal.GetMachineHost()}`\n" +
                $"The machine ID for this machine is: `{SystemGlobal.GetMachineId()}`\n" +
                $"The IP address for this machine is: `{NetworkingGlobal.GetLocalIp()}`\n" +
                "Please paste this into the `Host Name` field in grid-bot-support templates so that the internal team can easily identify this instance."
            );
        }
    }
}