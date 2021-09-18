using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class GetVersion : IStateSpecificCommandHandler
    {
        public string CommandName => "Get Version";

        public string CommandDescription => "Gets the Grid Server's version.";

        public string[] CommandAliases => new string[] { "gv" };

        public bool Internal => true;

        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            await message.ReplyAsync(await SoapUtility.Singleton.GetVersionAsync());
        }
    }
}
