using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Commands
{
    internal sealed class OpenGridServer : IStateSpecificCommandHandler
    {
        public string CommandName => "Open Grid Server";

        public string CommandDescription => "Opens the grid server";

        public string[] CommandAliases => new string[] { "ogsrv" };

        public bool Internal => true;

        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            SystemUtility.Singleton.OpenGridServer();

            await message.ReplyAsync("Successfully opened grid server!");
        }
    }
}
