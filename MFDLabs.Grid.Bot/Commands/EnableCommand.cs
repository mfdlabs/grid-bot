using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Registries;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Grid.Bot.Commands
{
    internal sealed class EnableCommand : IStateSpecificCommandHandler
    {
        public string CommandName => "Enabled Bot Command";
        public string CommandDescription => $"Tries to enable a command from the CommandRegistry\nLayout:" +
                                            $"{MFDLabs.Grid.Bot.Properties.Settings.Default.Prefix}enable commandName.";
        public string[] CommandAliases => new[] { "enable" };
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

            if (!CommandRegistry.SetIsEnabled(commandName, true))
            {
                await message.ReplyAsync($"The command by the nameof '{commandName}' was not found.");
                return;
            }

            await message.ReplyAsync($"Successfully enabled the command '{commandName}'.");
        }
    }
}
