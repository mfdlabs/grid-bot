using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;

namespace MFDLabs.Grid.Bot.Commands
{
    internal sealed class BatchOpenGridServers : IStateSpecificCommandHandler
    {
        public string CommandName => "Batch Open Grid Servers";
        public string CommandDescription => $"Attempts to batch open grid servers\nLayout:" +
                                            $"{(global::MFDLabs.Grid.Bot.Properties.Settings.Default.Prefix)}batchinstance " +
                                            $"unsafe?=false count?=1";
        public string[] CommandAliases => new[] { "batch", "batchinstance" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            if (global::MFDLabs.Grid.Properties.Settings.Default.SingleInstancedGridServer)
            {
                await message.ReplyAsync("Not opening any instances due to single-instanced environment.");
                return;
            }

            if (!bool.TryParse(messageContentArray.ElementAtOrDefault(0), out bool @unsafe))
                @unsafe = false;

            if (!int.TryParse(messageContentArray.ElementAtOrDefault(1), out int count))
                count = 1;

            if (count < 1)
            {
                await message.ReplyAsync("The instance count is required to be above 0.");
                return;
            }

            if (@unsafe)
                GridServerArbiter.Singleton.BatchQueueUpArbiteredInstancesUnsafe(count);
            else
                GridServerArbiter.Singleton.BatchQueueUpArbiteredInstances(count);

            if (@unsafe)
                await message.ReplyAsync($"Successfully dequeued {count} of grid server instances for immediate startup.");
            else
                await message.ReplyAsync($"Successfully opened {count} of grid server instances.");
        }
    }
}
