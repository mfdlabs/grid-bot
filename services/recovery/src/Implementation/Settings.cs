namespace Grid.Bot;

using System;

using Logging;
using Configuration;

/// <summary>
/// Settings for the recovery service.
/// </summary>
public class Settings : EnvironmentProvider, ISettings
{
    /// <inheritdoc cref="ISettings.GridBotEndpoint"/>
    public string GridBotEndpoint => GetOrDefault<string>(
        nameof(GridBotEndpoint), 
        () => throw new ApplicationException($"{nameof(GridBotEndpoint)} is required.")
    );

    /// <inheritdoc cref="ISettings.BotToken"/>
    public string BotToken => GetOrDefault<string>(
        nameof(BotToken), 
        () => throw new ApplicationException($"{nameof(BotToken)} is required.")
    );

    /// <inheritdoc cref="ISettings.MaintenanceStatusMessage"/>
    public string MaintenanceStatusMessage => GetOrDefault(
        nameof(MaintenanceStatusMessage), 
        "Service experiencing issues, please try again later."
    );

    /// <inheritdoc cref="ISettings.BotPrefix"/>
    public string BotPrefix => GetOrDefault(nameof(BotPrefix), ">");

    /// <inheritdoc cref="ISettings.PreviousPhaseCommands"/>
    public string[] PreviousPhaseCommands => GetOrDefault(nameof(PreviousPhaseCommands), Array.Empty<string>());

    /// <inheritdoc cref="ISettings.DefaultLoggerName"/>
    public string DefaultLoggerName => GetOrDefault(nameof(DefaultLoggerName), "recovery");

    /// <inheritdoc cref="ISettings.DefaultLoggerLevel"/>
    public LogLevel DefaultLoggerLevel => GetOrDefault(nameof(DefaultLoggerLevel), LogLevel.Information);

    /// <inheritdoc cref="ISettings.MetricsServerPort"/>
    public int MetricsServerPort => GetOrDefault(nameof(MetricsServerPort), 8080);

    /// <inheritdoc cref="ISettings.StandaloneMode"/>
    public bool StandaloneMode => GetOrDefault(nameof(StandaloneMode), false);

    /// <inheritdoc cref="ISettings.BotCheckWorkerDelay"/>
    public TimeSpan BotCheckWorkerDelay => GetOrDefault(nameof(BotCheckWorkerDelay), TimeSpan.FromSeconds(15));

    /// <inheritdoc cref="ISettings.MaxContinuousFailures"/>
    public int MaxContinuousFailures => GetOrDefault(nameof(MaxContinuousFailures), 2);

    /// <inheritdoc cref="ISettings.AlertRoleId"/>
    public ulong AlertRoleId => GetOrDefault<ulong>(nameof(AlertRoleId), default(ulong));

    /// <inheritdoc cref="ISettings.DiscordWebhookUrl"/>
    public string DiscordWebhookUrl => GetOrDefault<string>(
        nameof(DiscordWebhookUrl), 
        () => throw new ApplicationException($"{nameof(DiscordWebhookUrl)} is required.")
    );

    /// <inheritdoc cref="ISettings.GrpcClientUseTls"/>
    public bool GrpcClientUseTls => GetOrDefault(nameof(GrpcClientUseTls), false);
}
