using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Diagnostics;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Networking;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class GetMachineAddress : IStateSpecificCommandHandler
    {
        public string CommandName => "Get Bot Machine Address";
        public string CommandDescription => "Attempts to Query the current machine of the bot's information, like the LocalIP, the MachineHost and the MachineID.";
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
