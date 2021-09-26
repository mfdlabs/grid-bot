using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class GetVersion : IStateSpecificCommandHandler
    {
        public string CommandName => "Get Grid Server Version";
        public string CommandDescription => "Attempts to fetch the Grid Server's version via SoapUtility.";
        public string[] CommandAliases => new string[] { "gv", "getversion" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            await message.ReplyAsync(await SoapUtility.Singleton.GetVersionAsync());
        }
    }
}
