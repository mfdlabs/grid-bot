using Discord.WebSocket;
using MFDLabs.Diagnostics;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Networking;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class GetMachineAddress : IStateSpecificCommandHandler
    {
        public string CommandName => "Get Machine Address";

        public string CommandDescription => "Gets the machine's address, IP address and name.";

        public string[] CommandAliases => new string[] { "gaddr", "getaddress" };

        public bool Internal => true;

        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            await message.ReplyAsync($"Query Request ID: {NetworkingGlobal.Singleton.GenerateUUIDV4()}: {NetworkingGlobal.Singleton.GetLocalIP()} ({SystemGlobal.Singleton.GetMachineHost()}) - {SystemGlobal.Singleton.GetMachineID()}");
        }
    }
}
