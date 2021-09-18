using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class KillGridServer : IStateSpecificCommandHandler
    {
        public string CommandName => "Kill grid server";

        public string CommandDescription => "Kills the environment's grid server";

        public string[] CommandAliases => new string[] { "kgsrv", "killgridserver" };

        public bool Internal => true;

        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            SystemUtility.Singleton.KillGridServerSafe();
            await message.ReplyAsync("successfully killed grid server");
        }
    }
}
