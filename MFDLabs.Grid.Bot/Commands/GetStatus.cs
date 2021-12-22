using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class GetStatus : IStateSpecificCommandHandler
    {
        public string CommandName => "Get Grid Server Status";
        public string CommandDescription => "Attempts to get the grid server status via SoapUtility.";
        public string[] CommandAliases => new[] { "gs", "getstatus" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            await message.ReplyAsync((await SoapUtility.Singleton.GetStatusAsync()).ToJson());
        }
    }
}
