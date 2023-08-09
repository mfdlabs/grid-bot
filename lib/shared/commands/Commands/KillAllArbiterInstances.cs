using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Grid.Bot.Extensions;
using Grid.Bot.Interfaces;
using Grid.Bot.Utility;

namespace Grid.Bot.Commands
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


            var totalItemsKilled = GridServerArbiter.Singleton.KillAllInstances();

            if (totalItemsKilled == 0)
            {
                await message.ReplyAsync("No instances were killed because no instances were open!");
                return;
            }
            
            await message.ReplyAsync($"Successfully closed {totalItemsKilled} arbiter instances.");
        }
    }
}
