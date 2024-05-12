namespace Grid.Bot.Events;

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

#if DEBUG
using System.Reflection;
#endif

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Logging;

using Prometheus;

/// <summary>
/// Event handler for messages.
/// </summary>
/// <remarks>
/// Construct a new instance of <see cref="OnMessage"/>.
/// </remarks>
/// <param name="settings">The <see cref="ISettings"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
/// <exception cref="ArgumentNullException">
/// - <paramref name="settings"/> cannot be null.
/// - <paramref name="logger"/> cannot be null.
/// </exception>
public partial class OnMessage(
    ISettings settings,
    ILogger logger
)
{
    // language=regex
    private const string _allowedCommandRegex = @"^[a-zA-Z-]*$";

    [GeneratedRegex(_allowedCommandRegex)]
    private static partial Regex GetAllowedCommandRegex();

    private readonly ISettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly Counter _totalMessagesProcessed = Metrics.CreateCounter(
        "grid_messages_processed_total",
        "The total number of messages processed."
    );

    /// <summary>
    /// Invoke the event handler.
    /// </summary>
    /// <param name="rawMessage">The <see cref="SocketMessage"/></param>
    public async Task Invoke(SocketMessage rawMessage)
    {
        if (rawMessage is not SocketUserMessage message) return;
        if (message.Author.IsBot) return;

        _totalMessagesProcessed.Inc();

        int argPos = 0;

        if (!message.HasStringPrefix(_settings.BotPrefix, ref argPos, StringComparison.OrdinalIgnoreCase)) return;

        // Get the name of the command that was used.
        var commandName = message.Content.Split(' ')[0];
        if (string.IsNullOrEmpty(commandName)) return;

        commandName = commandName[argPos..];
        if (string.IsNullOrEmpty(commandName)) return;
        if (!GetAllowedCommandRegex().IsMatch(commandName)) return;
        if (!_settings.PreviousPhaseCommands.Contains(commandName.ToLowerInvariant())) return;

        _logger.Warning(
            "User tried to use previous phase command '{0}'.",
            commandName
        );

#if DEBUG

        var entryAssembly = Assembly.GetEntryAssembly();
        var informationalVersion = entryAssembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (string.IsNullOrEmpty(informationalVersion))
            informationalVersion = entryAssembly?.GetName().Version?.ToString();

        if (!string.IsNullOrEmpty(informationalVersion))
            await message.Channel.SendMessageAsync($"Debug build running version {informationalVersion}.");

#endif

        var failureMessage = _settings.MaintenanceStatusMessage;

        var embed = new EmbedBuilder()
            .WithTitle("Maintenance Enabled")
            .WithColor(Color.Red)
            .WithCurrentTimestamp();

        if (!string.IsNullOrEmpty(failureMessage))
            embed.WithDescription(failureMessage);

        await message.ReplyAsync("Maintenance is currently enabled, please try again later.", embeds: [embed.Build()]);

        return;
    }
}
