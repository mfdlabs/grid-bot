using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;
using MFDLabs.Text.Extensions;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class GetStatus : IStateSpecificCommandHandler
    {
        public string CommandName => "Get Status";

        public string CommandDescription => "Gets the Grid Server's status.";

        public string[] CommandAliases => new string[] { "gs" };

        public bool Internal => true;

        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            await message.ReplyAsync((await SoapUtility.Singleton.GetStatusAsync()).ToJson());
        }
    }
}
