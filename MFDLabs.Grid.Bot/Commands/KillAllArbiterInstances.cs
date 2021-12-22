using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class KillAllArbiterInstances : IStateSpecificCommandHandler
    {
        public string CommandName => "Kill All Arbiter Instances";
        public string CommandDescription => "Attempts to kill all the arbiter instances, if none are running, it says that.";
        public string[] CommandAliases => new[] { "kaai", "killallarbiterinstances" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer)
            {
                await message.ReplyAsync("Not closing any instances, we are in a single instanced environment.");
                return;
            }

            if (!bool.TryParse(messageContentArray.ElementAtOrDefault(0), out var @unsafe))
                @unsafe = false;

            var totalItemsKilled = @unsafe
                ? GridServerArbiter.Singleton.KillAllOpenInstancesUnsafe()
                : GridServerArbiter.Singleton.KillAllOpenInstances();

            if (totalItemsKilled == 0)
            {
                await message.ReplyAsync("No instances were killed because no instances were open!");
                return;
            }
            await message.ReplyAsync($"Successfully closed {totalItemsKilled} arbiter instances.");
        }
    }
}
