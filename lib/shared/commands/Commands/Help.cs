namespace Grid.Bot.Commands;

using System;
using System.Linq;
using System.Threading.Tasks;

using Discord.WebSocket;

using Extensions;
using Interfaces;
using Registries;

/// <summary>
/// Echoes back an embed that provides information about commands.
/// </summary>
[Obsolete("Text-based commands are being deprecated. Please begin to use slash commands!")]
internal class Help : ICommandHandler
{
    /// <inheritdoc cref="ICommandHandler.Name"/>
    public string Name => "Bot Help";

    /// <inheritdoc cref="ICommandHandler.Description"/>
    public string Description => "Attempts to return an embed on a State Specific command by name, or all commands within the user's permission scope, " +
        $"if the command doesn't exist, you will get an error\n" +
        $"Layout: {CommandsSettings.Singleton.Prefix}help commandName?";

    /// <inheritdoc cref="ICommandHandler.Aliases"/>
    public string[] Aliases => new[] { "h", "help" };

    /// <inheritdoc cref="ICommandHandler.IsInternal"/>
    public bool IsInternal => false;

    /// <inheritdoc cref="ICommandHandler.IsEnabled"/>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc cref="ICommandHandler.ExecuteAsync(string[], SocketMessage, string)"/>
    public async Task ExecuteAsync(string[] messageContentArray, SocketMessage message, string originalCommand)
    {
        var commandName = messageContentArray.ElementAtOrDefault(0);

        if (commandName != default)
        {
            var embed = CommandRegistry.ConstructHelpEmbedForSingleCommand(commandName, message.Author);

            if (embed == null)
            {
                await message.ReplyAsync($"The command with the name '{commandName}' was not found.");

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
