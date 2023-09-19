namespace Grid.Bot.Commands;

using System;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using Extensions;
using Interfaces;
using Registries;

/// <summary>
/// Echoes an embed with metrics collected by <see cref="CommandRegistry"/>
/// </summary>
[Obsolete("Text-based commands are being deprecated. Please begin to use slash commands!")]
internal class Metrics : ICommandHandler
{
    /// <inheritdoc cref="ICommandHandler.Name"/>
    public string Name => "Get bot metrics";

    /// <inheritdoc cref="ICommandHandler.Description"/>
    public string Description => "Gets an embed of the current metrics for the bot instance.";

    /// <inheritdoc cref="ICommandHandler.Aliases"/>
    public string[] Aliases => new[] { "metrics", "stats" };

    /// <inheritdoc cref="ICommandHandler.IsInternal"/>
    public bool IsInternal => true;

    /// <inheritdoc cref="ICommandHandler.IsEnabled"/>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc cref="ICommandHandler.ExecuteAsync(string[], SocketMessage, string)"/>
    public async Task ExecuteAsync(string[] messageContentArray, SocketMessage message, string originalCommand)
    {
        if (!await message.RejectIfNotAdminAsync()) return;

        var (modes, counters) = CommandRegistry.GetMetrics();

        var embed = new EmbedBuilder()
            .WithTitle("Bot Instance Metrics")
            .WithCurrentTimestamp()
            .AddField(
                "Counters",
                $"Total: {counters.RequestCountN}\n" +
                $"Succeeded: {counters.RequestSucceededCountN}\n" +
                $"Failed: {counters.RequestFailedCountN}",
                true
            )
            .AddField(
                "Modes",
                $"Average user: {modes.Users.Item} at {modes.Users.Average}\n" +
                $"Average guild: {modes.Servers.Item} at {modes.Servers.Average}\n" +
                $"Average command: {modes.Commands.Item} at {modes.Commands.Average}",
                true
            )
            .Build();

        await message.ReplyAsync(
            $"Command Registry metrics report for {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.ffffZ}",
            embed: embed
        );
    }
}
