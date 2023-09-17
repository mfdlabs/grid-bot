namespace Grid.Bot.Commands;

using System;
using System.Linq;
using System.Threading.Tasks;

using Discord.WebSocket;

using Text.Extensions;

using Extensions;
using Interfaces;
using Registries;

/// <summary>
/// Disables an <see cref="ICommandHandler"/>
/// </summary>
[Obsolete("Text-based commands are being deprecated. Please begin to use slash commands!")]
internal sealed class DisableCommand : ICommandHandler
{
    /// <inheritdoc cref="ICommandHandler.Name"/>
    public string Name => "Disable Bot Command";

    /// <inheritdoc cref="ICommandHandler.Description"/>
    public string Description => $"Tries to disable a command from the CommandRegistry\nLayout:" +
                                        $"{CommandsSettings.Singleton.Prefix}disable commandName.";

    /// <inheritdoc cref="ICommandHandler.Aliases"/>
    public string[] Aliases => new[] { "disable" };

    /// <inheritdoc cref="ICommandHandler.IsInternal"/>
    public bool IsInternal => true;

    /// <inheritdoc cref="ICommandHandler.IsEnabled"/>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc cref="ICommandHandler.ExecuteAsync(string[], SocketMessage, string)"/>
    public async Task ExecuteAsync(string[] messageContentArray, SocketMessage message, string originalCommand)
    {
        if (!await message.RejectIfNotAdminAsync()) return;

        var commandName = messageContentArray.ElementAtOrDefault(0);

        if (commandName.IsNullOrEmpty())
        {
            await message.ReplyAsync("The command name is required.");
            return;
        }

        var disabledMessage = string.Join(" ", messageContentArray.Skip(1));

        if (!CommandRegistry.SetIsEnabled(commandName, false, disabledMessage.IsNullOrEmpty() ? null : disabledMessage))
        {
            await message.ReplyAsync($"The command by the nameof '{commandName}' was not found.");

            return;
        }

        await message.ReplyAsync($"Successfully disabled the command '{commandName}'.");
    }
}
