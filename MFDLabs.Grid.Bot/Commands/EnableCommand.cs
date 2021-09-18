using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Registries;
using MFDLabs.Text.Extensions;
using System.Linq;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Commands
{
    internal sealed class EnableCommand : IStateSpecificCommandHandler
    {
        public string CommandName => "Enabled Command";

        public string CommandDescription => "Enables a command by name.";

        public string[] CommandAliases => new string[] { "enable" };

        public bool Internal => true;

        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            var commandName = messageContentArray.ElementAtOrDefault(0);

            if (commandName.IsNullOrEmpty())
            {
                await message.ReplyAsync("The command name is required.");
                return;
            }

            if (!CommandRegistry.Singleton.SetIsEnabled(commandName, true))
            {
                await message.ReplyAsync($"The command by the nameof '{commandName}' was not found.");
                return;
            }

            await message.ReplyAsync($"Successfully enabled the command '{commandName}'.");
        }
    }
}
