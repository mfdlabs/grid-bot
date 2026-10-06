namespace Grid.Bot;

using System;

using Logging;

/// <summary>
/// Options for the recovery service.
/// </summary>
public class RecoveryOptions
{
    /// <summary>
    /// Gets or sets the endpoint for the gRPC grid-bot service. Required unless <see cref="StandaloneMode"/> is enabled.
    /// </summary>
    public string GridBotEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the token for the bot.
    /// </summary>
    public string BotToken { get; set; }

    /// <summary>
    /// Gets or sets the maintenance status message.
    /// </summary>
    public string MaintenanceStatusMessage { get; set; } = "Service experiencing issues, please try again later.";

    /// <summary>
    /// Gets or sets the bot prefix (previous phase commands).
    /// </summary>
    public string BotPrefix { get; set; } = ">";

    /// <summary>
    /// Gets or sets the list of previous phase commands.
    /// </summary>
    public string[] PreviousPhaseCommands { get; set; } = [];

    /// <summary>
    /// Gets or sets the default logger name.
    /// </summary>
    public string DefaultLoggerName { get; set; } = "recovery";

    /// <summary>
    /// Gets or sets the default logger level.
    /// </summary>
    public LogLevel DefaultLoggerLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets or sets the metrics server port.
    /// </summary>
    public int MetricsServerPort { get; set; } = 8080;

    /// <summary>
    /// Gets or sets a value indicating whether standalone mode is enabled.
    /// </summary>
    public bool StandaloneMode { get; set; }

    /// <summary>
    /// Gets or sets the delay between each bot check worker run.
    /// </summary>
    public TimeSpan BotCheckWorkerDelay { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Gets or sets the max amount of continuous failures before the bot check worker enables maintenance mode.
    /// </summary>
    public int MaxContinuousFailures { get; set; } = 2;

    /// <summary>
    /// Gets or sets the role Id for the alert role.
    /// </summary>
    public ulong AlertRoleId { get; set; }

    /// <summary>
    /// Gets or sets the Discord webhook URL for alerts. Required unless <see cref="StandaloneMode"/> is enabled.
    /// </summary>
    public string DiscordWebhookUrl { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the gRPC client should use TLS.
    /// </summary>
    public bool GrpcClientUseTls { get; set; }
}
