namespace Grid.Bot;

using System;

using Discord;
using Logging;

/// <summary>
/// Settings provider for all Discord related stuff.
/// </summary>
public class DiscordSettings : BaseSettingsProvider<DiscordSettings>
{
    /// <inheritdoc cref="Configuration.IVaultProvider.Path"/>
    public override string Path => SettingsProvidersDefaults.DiscordPath;

    /// <summary>
    /// Gets the bot token.
    /// </summary>
    /// <exception cref="InvalidOperationException">The setting is required!</exception>
    public string BotToken => GetOrDefault<string>(
        nameof(BotToken),
        () => throw new InvalidOperationException($"Environment Variable {nameof(BotToken)} is required!")
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
}
