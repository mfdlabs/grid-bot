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
/// Enables an <see cref="ICommandHandler"/>
/// </summary>
[Obsolete("Text-based commands are being deprecated. Please begin to use slash commands!")]
internal sealed class EnableCommand : ICommandHandler
{
    /// <inheritdoc cref="ICommandHandler.Name"/>
    public string Name => "Enabled Bot Command";

    /// <inheritdoc cref="ICommandHandler.Description"/>
    public string Description => $"Tries to enable a command from the CommandRegistry\nLayout:" +
                                        $"{CommandsSettings.Singleton.Prefix}enable commandName.";

    /// <inheritdoc cref="ICommandHandler.Aliases"/>
    public string[] Aliases => new[] { "enable" };

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

        if (!CommandRegistry.SetIsEnabled(commandName, true))
        {
            await message.ReplyAsync($"The command by the nameof '{commandName}' was not found.");
            return;
        }

        await message.ReplyAsync($"Successfully enabled the command '{commandName}'.");
    }
}
