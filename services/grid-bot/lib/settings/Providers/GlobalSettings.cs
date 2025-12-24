namespace Grid.Bot;

using System;

using Configuration;

using Logging;

/// <summary>
/// Settings provider for global entrypoint stuff.
/// </summary>
public class GlobalSettings : BaseSettingsProvider
{
    /// <inheritdoc cref="IVaultProvider.Path"/>
    public override string Path => SettingsProvidersDefaults.GlobalPath;

    /// <summary>
    /// Gets the name of the default logger.
    /// </summary>
    public string DefaultLoggerName => GetOrDefault(
        nameof(DefaultLoggerName),
        "bot"
    );

    /// <summary>
    /// Gets the log level for the default logger.
    /// </summary>
    public LogLevel DefaultLoggerLevel => GetOrDefault(
        nameof(DefaultLoggerLevel),
        LogLevel.Information
    );

    /// <summary>
    /// Gets the address for the metrics server.
    /// </summary>
    public string MetricsBindAddress => GetOrDefault(
        nameof(MetricsBindAddress),
        "http://+:8081/"
    );

    /// <summary>
    /// Should the default logger log to console?
    /// </summary>
    public bool DefaultLoggerLogToConsole => GetOrDefault(
        nameof(DefaultLoggerLogToConsole),
        true
    );

    /// <summary>
    /// Gets the Discord URL for the primary support guild.
    /// </summary>
    public string SupportGuildDiscordUrl => GetOrDefault(
        nameof(SupportGuildDiscordUrl),
        "https://discord.gg/hdg2z6bm5c"
    );

    /// <summary>
    /// Gets the GitHub url for the support hub.
    /// </summary>
    public string SupportHubGitHubUrl => GetOrDefault(
        nameof(SupportHubGitHubUrl),
        "https://github.com/mfdlabs/grid-bot-support"
    );

    /// <summary>
    /// Gets the Url for the documentation hub.
    /// </summary>
    public string DocumentationHubUrl => GetOrDefault(
        nameof(DocumentationHubUrl),
        "https://grid-bot.ops.vmminfra.net"
    );

    /// <summary>
    /// Is alerting via Discord webhook enabled?
    /// </summary>
    public bool DiscordWebhookAlertingEnabled => GetOrDefault(
        nameof(DiscordWebhookAlertingEnabled),
        false
    );

    /// <summary>
    /// Gets the Discord webhook URL for alerts.
    /// </summary>
    public string DiscordWebhookUrl => GetOrDefault(
        nameof(DiscordWebhookUrl),
        string.Empty
    );
}
