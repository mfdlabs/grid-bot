using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class KillWebServer : IStateSpecificCommandHandler
    {
        public string CommandName => "Shutdown Web Server";
        public string CommandDescription => "Attempts to shutdown the web server, if it was no running, or was running on a higher context than us, it will tell the frontend user.";
        public string[] CommandAliases => new string[] { "kwsrv", "killwebserver" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            if (!SystemUtility.Singleton.KillGridServerSafe())
            {
                await message.ReplyAsync("The Web Server was not closed because it was either not running, or on a higher context than the current application.");
                return;
            }
            await message.ReplyAsync("successfully killed Web server");
        }
    }
}
