namespace Grid.Bot;

using System;

using Logging;

/// <summary>
/// Settings for the recovery service.
/// </summary>
public interface ISettings
{
    /// <summary>
    /// Gets the endpoint for the gRPC grid-bot service.
    /// </summary>
    string GridBotEndpoint { get; }

    /// <summary>
    /// Gets the token for the bot.
    /// </summary>
    string BotToken { get; }

    /// <summary>
    /// Gets the maintenance status message.
    /// </summary>
    string MaintenanceStatusMessage { get; }

    /// <summary>
    /// Gets the bot prefix (previous phase commands).
    /// </summary>
    string BotPrefix { get; }

    /// <summary>
    /// Gets the list of previous phase commands.
    /// </summary>
    string[] PreviousPhaseCommands { get; }

    /// <summary>
    /// Default logger name.
    /// </summary>
    string DefaultLoggerName { get; }

    /// <summary>
    /// Default logger level.
    /// </summary>
    LogLevel DefaultLoggerLevel { get; }

    /// <summary>
    /// Gets the metrics server port.
    /// </summary>
    int MetricsServerPort { get; }

    /// <summary>
    /// Determines if standalone mode is enabled.
    /// </summary>
    bool StandaloneMode { get; }

    /// <summary>
    /// Gets the delay between each bot check worker run.
    /// </summary>
    TimeSpan BotCheckWorkerDelay { get; }

    /// <summary>
    /// Gets the max amount of continuous failures before the bot check worker enables maintenance mode.
    /// </summary>
    int MaxContinuousFailures { get; }

    /// <summary>
    /// Gets role Id for the alert role.
    /// </summary>
    ulong AlertRoleId { get; }

    /// <summary>
    /// Gets the Discord webhook URL for alerts.
    /// </summary>
    string DiscordWebhookUrl { get; }
}
