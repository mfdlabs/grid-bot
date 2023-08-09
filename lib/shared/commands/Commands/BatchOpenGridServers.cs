using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Grid.Bot.Extensions;
using Grid.Bot.Interfaces;
using Grid.Bot.Utility;

namespace Grid.Bot.Commands
{
    internal sealed class BatchOpenGridServers : IStateSpecificCommandHandler
    {
        public string CommandName => "Batch Open Grid Servers";
        public string CommandDescription => $"Attempts to batch open grid servers\nLayout:" +
                                            $"{(global::Grid.Bot.Properties.Settings.Default.Prefix)}batchinstance " +
                                            $"count?=1";
        public string[] CommandAliases => new[] { "batch", "batchinstance" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            if (!int.TryParse(messageContentArray.ElementAtOrDefault(1), out int count))
                count = 1;

            if (count < 1)
            {
                await message.ReplyAsync("The instance count is required to be above 0.");
                return;
            }

            GridServerArbiter.Singleton.BatchCreateLeasedInstances(count: count);

            await message.ReplyAsync($"Successfully opened {count} of grid server instances.");
        }
    }
}
