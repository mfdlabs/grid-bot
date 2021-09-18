using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Models;
using MFDLabs.Grid.Bot.Tasks;
using MFDLabs.Logging;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class Render : IStateSpecificCommandHandler
    {
        public string CommandName => "Render";

        public string CommandDescription => "Renders a user.";

        public string[] CommandAliases => new string[] { "r", "render", "sexually-weird-render" };

        public bool Internal => !Settings.Singleton.RenderingEnabled;

        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!Settings.Singleton.RenderingEnabled)
            {
                if (!await message.RejectIfNotAdminAsync()) return;
            }

            SystemLogger.Singleton.Debug("Dispatching '{0}' to '{1}' for processing.", typeof(RenderTaskRequest).FullName, typeof(RenderQueueUserMetricsTask).FullName);

            RenderQueueUserMetricsTask.Singleton.Port.Post(new RenderTaskRequest()
            {
                ContentArray = messageContentArray,
                Message = message,
                OriginalCommandName = originalCommand
            });
        }
    }
}
