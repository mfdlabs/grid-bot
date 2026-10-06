namespace Grid.Bot;

using Discord;

using Logging;

/// <summary>
/// Options for all Discord related settings.
/// </summary>
public class DiscordOptions
{
    /// <summary>
    /// The configuration section these options bind from.
    /// </summary>
    public const string SectionName = "Discord";

    /// <summary>
    /// The vault path for the Discord options.
    /// </summary>
    public const string VaultPath = "discord";

    /// <summary>
    /// Gets or sets the bot token. Required in release builds.
    /// </summary>
    public string BotToken { get; set; } = string.Empty;

#if DEBUG || DEBUG_LOGGING_IN_PROD
    /// <summary>
    /// Gets or sets a value indicating whether task cancelled exceptions can be logged.
    /// </summary>
    public bool DebugAllowTaskCanceledExceptions { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether websocket or gateway exceptions can be logged.
    /// </summary>
    public bool DebugAllowGatewayWebsocketExceptions { get; set; }
#endif

#if DEBUG
    /// <summary>
    /// Gets or sets the ID of the guild to register slash commands in.
    /// </summary>
    /// <remarks>
    /// This is only valid for debug builds.
    /// If this is 0, then the bot will not register slash commands.
    /// </remarks>
    public ulong DebugGuildId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the bot is disabled.
    /// </summary>
    /// <remarks>
    /// This is only valid for debug builds.
    /// </remarks>
    public bool DebugBotDisabled { get; set; }
#endif

    /// <summary>
    /// Gets or sets a value indicating whether Discord internals are logged.
    /// </summary>
    public bool ShouldLogDiscordInternals { get; set; } = true;

    /// <summary>
    /// Gets or sets the <see cref="UserStatus"/> of the bot.
    /// </summary>
    public UserStatus BotStatus { get; set; } = UserStatus.Online;

    /// <summary>
    /// Gets or sets the status message of the bot.
    /// </summary>
    public string BotStatusMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the Discord logger.
    /// </summary>
    public string DiscordLoggerName { get; set; } = "discord";

    /// <summary>
    /// Gets or sets the <see cref="Logging.LogLevel"/> of the Discord logger.
    /// </summary>
    public LogLevel DiscordLoggerLogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets or sets a value indicating whether the Discord logger logs to console.
    /// </summary>
    public bool DiscordLoggerLogToConsole { get; set; } = true;
}
