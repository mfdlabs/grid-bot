using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Models;
using MFDLabs.Grid.Bot.Tasks;
using MFDLabs.Logging;

namespace MFDLabs.Grid.Bot.Commands
{
    internal sealed class GridServerExecutionCommandTest : IStateSpecificCommandHandler
    {
        public string CommandName => "Grid Server Execution Command Test";
        public string CommandDescription => "A test at Lua execution to a remove grid server instance via SoapUtility.";
        public string[] CommandAliases => new[] {"t", "test"};
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            SystemLogger.Singleton.Debug("Dispatching '{0}' to '{1}' for processing.",
                typeof(SocketTaskRequest).FullName,
                typeof(ScriptExecutionQueueUserMetricsTask).FullName);

            ScriptExecutionQueueUserMetricsTask.Singleton.Port.Post(new SocketTaskRequest()
            {
                ContentArray = new [] { "return 1, 2, 3" },
                Message = message,
                OriginalCommandName = originalCommand
            });
        }
    }
}