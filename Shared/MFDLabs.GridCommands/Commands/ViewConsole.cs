using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Diagnostics;
using MFDLabs.Grid.Bot.Models;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.WorkQueues;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class ViewConsole : IStateSpecificCommandHandler
    {
        public string CommandName => "View Grid Server Console";
        public string CommandDescription => "Dispatches a 'ScreenshotTask' request to the task thread port." +
                                            " Will try to screenshot the current grid server's console output.";
        public string[] CommandAliases => new[] { "vc", "viewconsole" };
        public bool Internal => false;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            var request = new SocketTaskRequest
            {
                ContentArray = messageContentArray,
                Message = message,
                OriginalCommandName = originalCommand
            };

            if (!PercentageInvoker.InvokeAction(
                () => GridServerScreenshotWorkQueue.Singleton.EnqueueWorkItem(request),
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.NewViewGridServerConsoleWorkQueueRolloutPercentage
            ))
            {
                if (message.Author.IsAdmin())
                {
                    GridServerScreenshotWorkQueue.Singleton.EnqueueWorkItem(request);
                    return;
                }
                await message.ReplyAsync("View console is not enabled at this time.");
                return;
            }
        }
    }
}
