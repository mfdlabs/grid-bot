using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Tasks;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class ViewConsole : IStateSpecificCommandHandler
    {
        public string CommandName => "View console";

        public string CommandDescription => "View the grid server's console output.";

        public string[] CommandAliases => new string[] { "vc", "viewconsole" };

        public bool Internal => !Settings.Singleton.ViewConsoleEnabled;

        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!Settings.Singleton.ViewConsoleEnabled)
            {
                if (!await message.RejectIfNotAdminAsync()) return;
            }

            ScreenshotTask.Singleton.Port.Post(message);
        }
    }
}
