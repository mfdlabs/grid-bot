using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class KillArbiterInstance : IStateSpecificCommandHandler
    {
        public string CommandName => "Shutdown an arbiter instance";
        public string CommandDescription => "Attempts to shutdown an arbiter instance, if it was no running, or was running on a higher context than us, it will tell the frontend user.";
        public string[] CommandAliases => new string[] { "kai", "killarbiterinstance" };
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

            var name = messageContentArray.Join(' ');

            if (name.IsNullOrWhiteSpace())
            {
                await message.ReplyAsync("The arbiter name cannot be null or whitespace!");
                return;
            }

            if (!GridServerArbiter.Singleton.KillInstanceByName(name))
            {
                await message.ReplyAsync($"The Arbiter Instance by the name of '{name}' was not found, so it wasn't closed.");
                return;
            }
            await message.ReplyAsync($"Successfully killed Arbiter Instance: '{name}'");
        }
    }
}
