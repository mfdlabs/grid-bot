using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Models;
using MFDLabs.Grid.Bot.Tasks;
using MFDLabs.Logging;

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

        public Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            SystemLogger.Singleton.Debug("Dispatching '{0}' to '{1}' for processing.",
                typeof(SocketTaskRequest).FullName,
                typeof(ScriptExecutionQueueUserMetricsTask).FullName);

            ScriptExecutionQueueUserMetricsTask.Singleton.Port.Post(new SocketTaskRequest()
            {
                ContentArray = messageContentArray,
                Message = message,
                OriginalCommandName = originalCommand
            });

            return Task.CompletedTask;
        }
    }
}
