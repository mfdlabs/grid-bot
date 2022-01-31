using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Diagnostics;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Models;
using MFDLabs.Grid.Bot.Tasks.WorkQueues;

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
                () => RenderingWorkQueue.Singleton.EnqueueWorkItem(request),
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderWorkQueueRolloutPercentage
            ))
            {
                if (message.Author.IsAdmin())
                {
                    RenderingWorkQueue.Singleton.EnqueueWorkItem(request);
                    return;
                }
                await message.ReplyAsync("Rendering is not enabled at this time.");
                return;
            }
        }
    }
}
