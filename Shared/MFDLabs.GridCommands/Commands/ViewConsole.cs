using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Models;
using MFDLabs.Grid.Bot.Tasks.WorkQueues;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class ViewConsole : IStateSpecificCommandHandler
    {
        public string CommandName => "View Grid Server Console";
        public string CommandDescription => "Dispatches a 'ScreenshotTask' request to the task thread port." +
                                            " Will try to screenshot the current grid server's console output.";
        public string[] CommandAliases => new[] { "vc", "viewconsole" };
        public bool Internal => !global::MFDLabs.Grid.Bot.Properties.Settings.Default.ViewConsoleEnabled;
        public bool IsEnabled { get; set; } = true;

        public Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            var request = new SocketTaskRequest
            {
                ContentArray = messageContentArray,
                Message = message,
                OriginalCommandName = originalCommand
            };

            GridServerScreenshotWorkQueue.Singleton.EnqueueWorkItem(request);

            return Task.CompletedTask;
        }
    }
}
