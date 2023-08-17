#if WE_LOVE_EM_SLASH_COMMANDS

using System.Net.Sockets;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Networking;
using Diagnostics;
using Grid.Bot.Extensions;
using Grid.Bot.Interfaces;
using Grid.Bot.Utility;

namespace Grid.Bot.SlashCommands
{
    internal sealed class Hostname : IStateSpecificSlashCommandHandler
    {
        public string CommandDescription => "Get Machine Host Name";
        public string Name => "hostname";
        public bool Internal => false;
        public bool IsEnabled { get; set; } = true;
        public SlashCommandOptionBuilder[] Options => null;

        public async Task Invoke(SocketSlashCommand command)
        {
            await command.RespondEphemeralAsync(
                $"The hostname for this instance is: `{SystemGlobal.GetMachineHost()}`\n" +
                $"The machine ID for this machine is: `{SystemGlobal.GetMachineId()}`\n" +
                $"The IP address for this machine is: `{NetworkingGlobal.GetLocalIp(AddressFamily.InterNetwork)}`\n" +
                "Please paste this into the `Host Name` field in grid-bot-support templates so that the internal team can easily identify this instance."
            );
        }
    }
}

#endif
