using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Diagnostics;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Models;
using MFDLabs.Grid.Bot.Tasks;
using MFDLabs.Grid.Bot.Tasks.WorkQueues;
using MFDLabs.Logging;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class Render : IStateSpecificCommandHandler
    {
        public string CommandName => "Render User";
        public string CommandDescription => $"If no arguments are given, it will try to get the Roblox ID " +
                                            $"for the author and render them.\nLayout: " +
                                            $"{MFDLabs.Grid.Bot.Properties.Settings.Default.Prefix}render " +
                                            $"robloxUserID?|discordUserMention?|...userName?";
        public string[] CommandAliases => new[] { "r", "render", "sexually-weird-render" };
        public bool Internal => !global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderingEnabled;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderingEnabled)
            {
                if (!await message.RejectIfNotAdminAsync()) return;
            }

            var request = new SocketTaskRequest
            {
                ContentArray = messageContentArray,
                Message = message,
                OriginalCommandName = originalCommand
            };

            if (PercentageInvoker.InvokeAction(
                    () => RenderQueueUserMetricsWorkQueue.Singleton.EnqueueWorkItem(request),
                    global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderWorkQueueRolloutPercentage
                )
            ) return;

            SystemLogger.Singleton.Debug("Dispatching '{0}' to '{1}' for processing.",
                typeof(SocketTaskRequest).FullName,
                typeof(RenderQueueUserMetricsTask).FullName);

            RenderQueueUserMetricsTask.Singleton.Port.Post(new SocketTaskRequest
            {
                ContentArray = messageContentArray,
                Message = message,
                OriginalCommandName = originalCommand
            });
        }
    }
}
