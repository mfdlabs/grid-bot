namespace Grid.Bot.Commands;

using System;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using Global;
using Interfaces;
using Extensions;

/// <summary>
/// Gets the total count of Guilds that this Bot is in.
/// </summary>
[Obsolete("Text-based commands are being deprecated. Please begin to use slash commands!")]
internal sealed class Guilds : ICommandHandler
{
    /// <inheritdoc cref="ICommandHandler.Name"/>
    public string Name => "Get Guilds";

    /// <inheritdoc cref="ICommandHandler.Description"/>
    public string Description => "Gets the current bot's guilds";

    /// <inheritdoc cref="ICommandHandler.Aliases"/>
    public string[] Aliases => new[] { "guilds", "servers" };

    /// <inheritdoc cref="ICommandHandler.IsInternal"/>
    public bool IsInternal => true;

    /// <inheritdoc cref="ICommandHandler.IsEnabled"/>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc cref="ICommandHandler.ExecuteAsync(string[], SocketMessage, string)"/>
    public async Task ExecuteAsync(string[] messageContentArray, SocketMessage message, string originalCommand)
    {
        if (!await message.RejectIfNotAdminAsync()) return;

        await message.ReplyAsync(
            $"We are in {BotRegistry.Client.Guilds.Count} guilds!"
        );
    }
}
