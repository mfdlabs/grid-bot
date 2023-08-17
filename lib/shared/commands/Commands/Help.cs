using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Grid.Bot.Extensions;
using Grid.Bot.Interfaces;
using Grid.Bot.Registries;

namespace Grid.Bot.Commands
{
    internal class Help : IStateSpecificCommandHandler
    {
        public string CommandName => "Bot Help";
        public string CommandDescription => "Attempts to return an embed on a State Specific command by name, or all commands within the user's permission scope, " +
            $"if the command doesn't exist and the setting 'IsAllowedToEchoBackNotFoundCommandException' " +
            $"is enabled it will tell you it doesn't exist\nLayout: {Grid.Bot.Properties.Settings.Default.Prefix}help commandName?";
        public string[] CommandAliases => new[] { "h", "help" };
        public bool Internal => false;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            var commandName = messageContentArray.ElementAtOrDefault(0);

            if (commandName != default)
            {
                var embed = CommandRegistry.ConstructHelpEmbedForSingleCommand(commandName, message.Author);

                if (embed == null)
                {
                    if (global::Grid.Bot.Properties.Settings.Default.IsAllowedToEchoBackNotFoundCommandException)
                    {
                        await message.ReplyAsync($"The command with the name '{commandName}' was not found.");
                    }
                    return;
                }

                await message.Channel.SendMessageAsync(embed: embed);
                return;
            }

            var allCommandsEmbeds = CommandRegistry.ConstructHelpEmbedForAllCommands(message.Author);

            var count = 0;
            foreach (var embed in allCommandsEmbeds) count += embed.Fields.Length;

            await message.ReplyAsync($"Returning information on {count} commands.");

            foreach (var embed in allCommandsEmbeds)
                await message.Channel.SendMessageAsync(embed: embed);
        }
    }
}
