using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class KillWebServer : IStateSpecificCommandHandler
    {
        public string CommandName => "Kill web server";

        public string CommandDescription => "Kills the environment's web server";

        public string[] CommandAliases => new string[] { "kwebsrv", "killwebserver" };

        public bool Internal => true;

        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            SystemUtility.Singleton.KillServerSafe();
            await message.ReplyAsync("successfully killed web server");
        }
    }
}
