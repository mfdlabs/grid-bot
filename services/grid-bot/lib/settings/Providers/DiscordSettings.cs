namespace Grid.Bot;

using System;

using Discord;
using Logging;

/// <summary>
/// Settings provider for all Discord related stuff.
/// </summary>
public class DiscordSettings : BaseSettingsProvider
{
    /// <inheritdoc cref="Configuration.IVaultProvider.Path"/>
    public override string Path => SettingsProvidersDefaults.DiscordPath;

    /// <summary>
    /// Gets the bot token.
    /// </summary>
    /// <exception cref="InvalidOperationException">The setting is required!</exception>
    public string BotToken => GetOrDefault<string>(
        nameof(BotToken),
#if DEBUG
        string.Empty
#else
        () => throw new InvalidOperationException($"Environment Variable {nameof(BotToken)} is required!")
#endif
    );

#if DEBUG || DEBUG_LOGGING_IN_PROD
    /// <summary>
    /// Can task cancelled exceptions be loggeD?
    /// </summary>
    public bool DebugAllowTaskCanceledExceptions => GetOrDefault(
        nameof(DebugAllowTaskCanceledExceptions),
        false
    );
#endif

#if DEBUG

    /// <summary>
    /// Gets the ID of the guild to register slash commands in.
    /// </summary>
    /// <remarks>
    /// This is only valid for debug builds.
    /// If this is 0, then the bot will not register slash commands.
    /// </remarks>
    public ulong DebugGuildId => GetOrDefault(
        nameof(DebugGuildId),
        0UL
    );

    /// <summary>
    /// Determines if the bot should not be enabled.
    /// </summary>
    /// <remarks>
    /// This is only valid for debug builds.
    /// If this is true, then the bot will not be enabled.
    /// </remarks>
    public bool DebugBotDisabled => GetOrDefault(
        nameof(DebugBotDisabled),
        false
    );

#endif

    /// <summary>
    /// Should discord internals be logged?
    /// </summary>
    public bool ShouldLogDiscordInternals => GetOrDefault(
        nameof(ShouldLogDiscordInternals),
        true
    );

    /// <summary>
    /// Gets the <see cref="UserStatus"/> of the bot.
    /// </summary>
    public UserStatus BotStatus => GetOrDefault(
        nameof(BotStatus),
        UserStatus.Online
    );

    /// <summary>
    /// Gets the status message of the bot.
    /// </summary>
    public string BotStatusMessage => GetOrDefault(
        nameof(BotStatusMessage),
        string.Empty
    );

    /// <summary>
    /// Gets the name of the <see cref="ILogger"/>
    /// </summary>
    public string DiscordLoggerName => GetOrDefault(
        nameof(DiscordLoggerName),
        "discord"
    );

    /// <summary>
    /// Gets the <see cref="Logging.LogLevel"/> of the <see cref="ILogger"/>
    /// </summary>
    public LogLevel DiscordLoggerLogLevel => GetOrDefault(
        nameof(DiscordLoggerLogLevel),
        LogLevel.Information
    );

    /// <summary>
    /// Should the <see cref="ILogger"/> log to console?
    /// </summary>
    public bool DiscordLoggerLogToConsole => GetOrDefault(
        nameof(DiscordLoggerLogToConsole),
        true
    );
}
