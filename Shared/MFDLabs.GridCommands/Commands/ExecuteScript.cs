using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Diagnostics;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Models;
using MFDLabs.Grid.Bot.WorkQueues;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class ExecuteScript : IStateSpecificCommandHandler
    {
        public string CommandName => "Execute Grid Server Lua Script";
        public string CommandDescription => $"Attempts to execute the given script contents on a grid " +
                                            $"server instance\nLayout: {MFDLabs.Grid.Bot.Properties.Settings.Default.Prefix}execute ...script.";
        public string[] CommandAliases => new[] { "x", "ex", "execute" };
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
                () => ScriptExecutionWorkQueue.Singleton.EnqueueWorkItem(request),
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.NewScriptExecutionWorkQueueRolloutPercentage
            ))
            {
                if (message.Author.IsAdmin())
                {
                    ScriptExecutionWorkQueue.Singleton.EnqueueWorkItem(request);
                    return;
                }
                await message.ReplyAsync("Script execution is not enabled at this time.");
                return;
            }
        }
    }
}
