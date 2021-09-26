using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Registries;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class Help : IStateSpecificCommandHandler
    {
        public string CommandName => "Bot Help";
        public string CommandDescription => "Attempts to return an embed on a State Specific command by name, or all commands within the user's permission scope, " +
            $"if the command doesn't exist and the setting 'IsAllowedToEchoBackNotFoundCommandException' is enabled it will tell you it doesn't exist\nLayout: {Settings.Singleton.Prefix}help commandName?";
        public string[] CommandAliases => new string[] { "h", "help" };
        public bool Internal => false;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            var commandName = messageContentArray.ElementAtOrDefault(0);

            if (commandName != default)
            {
                var embed = CommandRegistry.Singleton.ConstructHelpEmbedForSingleCommand(commandName, message.Author);

                if (embed == null)
                {
                    if (Settings.Singleton.IsAllowedToEchoBackNotFoundCommandException)
                    {
                        await message.ReplyAsync($"The command with the name '{commandName}' was not found.");
                    }
                    return;
                }

                await message.Channel.SendMessageAsync(embed: embed);
                return;
            }

            var allCommandsEmbeds = CommandRegistry.Singleton.ConstructHelpEmbedForAllCommands(message.Author);

            var count = 0;
            foreach (var embed in allCommandsEmbeds) count += embed.Fields.Length;

            await message.ReplyAsync($"Returning information on {count} commands.");

            foreach (var embed in allCommandsEmbeds)
                await message.Channel.SendMessageAsync(embed: embed);
        }
    }
}
