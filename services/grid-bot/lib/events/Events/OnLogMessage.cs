namespace Grid.Bot.Events;

using System;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.Net;
using Discord.WebSocket;

using Discord.Commands;
using Discord.Interactions;

using Prometheus;

using Logging;

using Utility;

/// <summary>
/// Event invoked when Discord.Net creates a log message.
/// </summary>
public class OnLogMessage
{
    private readonly DiscordSettings _settings;

#if DEBUG || DEBUG_LOGGING_IN_PROD
    private readonly IDiscordWebhookAlertManager _discordWebhookAlertManager;
    private readonly IBacktraceUtility _backtraceUtility;
#endif

    private readonly Logger _logger;

    private readonly Counter _totalLogMessages = Metrics.CreateCounter(
        "bot_discord_log_messages_total",
        "The total number of log messages.",
        "log_severity"
    );

    private readonly Counter _totalSerializerErrors = Metrics.CreateCounter(
        "bot_discord_serializer_errors_total",
        "The total number of serializer errors."
    );

    private const string _serializerErrorMessage = "Serializer Error";

    // These are specific strings that fill the log files up drastically.
    private static readonly HashSet<string> _messagesToBeConsideredDebug =
    [
        "Disconnecting",
        "Disconnected",
        "Connecting",
        "Connected",
        "Resumed previous session"
    ];

#if DEBUG || DEBUG_LOGGING_IN_PROD
    /// <summary>
    /// Construct a new instance of <see cref="OnLogMessage"/>.
    /// </summary>
    /// <param name="settings">The <see cref="DiscordSettings"/>.</param>
    /// <param name="discordWebhookAlertManager">The <see cref="IDiscordWebhookAlertManager"/>.</param>
    /// <param name="backtraceUtility">The <see cref="IBacktraceUtility"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="settings"/> cannot be null.
    /// - <paramref name="discordWebhookAlertManager"/> cannot be null.
    /// - <paramref name="backtraceUtility"/> cannot be null.
    /// </exception>
    public OnLogMessage(DiscordSettings settings, IDiscordWebhookAlertManager discordWebhookAlertManager, IBacktraceUtility backtraceUtility)
#else
    /// <summary>
    /// Construct a new instance of <see cref="OnLogMessage"/>.
    /// </summary>
    /// <param name="settings">The <see cref="DiscordSettings"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> cannot be null.</exception>
    public OnLogMessage(DiscordSettings settings)
#endif
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

#if DEBUG || DEBUG_LOGGING_IN_PROD
        _discordWebhookAlertManager = discordWebhookAlertManager ?? throw new ArgumentNullException(nameof(discordWebhookAlertManager));
        _backtraceUtility = backtraceUtility ?? throw new ArgumentNullException(nameof(backtraceUtility));
#endif

        _logger = new Logger(
            name: _settings.DiscordLoggerName,
            logLevelGetter: () => _settings.DiscordLoggerLogLevel,
            logToConsole: _settings.DiscordLoggerLogToConsole
        );
    }

    /// <summary>
    /// Invoke the event handler.
    /// </summary>
    /// <param name="message">The <see cref="LogMessage"/></param>
    public Task Invoke(LogMessage message)
    {
        _totalLogMessages.WithLabels(message.Severity.ToString()).Inc();

        if (message.Exception != null)
        {
#if DEBUG || DEBUG_LOGGING_IN_PROD
            if (!_settings.DebugAllowGatewayWebsocketExceptions) {
                if (message.Exception is GatewayReconnectException)
                    return Task.CompletedTask;

                // Closed web socket exceptions are expected when the bot is shutting down.
                if (message.Exception.InnerException is WebSocketException)
                    return Task.CompletedTask;

                if (message.Exception is WebSocketClosedException || message.Exception.InnerException is WebSocketClosedException)
                    return Task.CompletedTask;
            }

            if (message.Exception is TaskCanceledException &&
                !_settings.DebugAllowTaskCanceledExceptions)
                return Task.CompletedTask;

            // Temporary fix for discord-net/Discord.Net#3128
            // Just keep it out of Backtrace and increment a counter.
            if (message.Message == _serializerErrorMessage)
            {
                _totalSerializerErrors.Inc();

                // Debug log the serializer error, in case we need to see it.
                _logger.Debug(
                    "Serializer Error, Source = {0}, Exception = {1}",
                    message.Source,
                    message.Exception.ToString()
                );

                return Task.CompletedTask;
            }

            if (message.Exception is InteractionException or CommandException) // Handled by the command handler.
                return Task.CompletedTask;

            _logger.Error(
                "Source = {0}, Message = {1}, Exception = {2}",
                message.Source,
                message.Message,
                message.Exception.ToString()
            );

            _backtraceUtility.UploadException(message.Exception);

            var content = $"""
                **Severity**: {message.Severity}
                **Source**: {message.Source}
                **Message**: {message.Message ?? "No message"}

                The global exception handler caught an exception:
                ```{message.Exception}```
                """;

            _discordWebhookAlertManager.SendAlertAsync(
                "Discord.Net Log Message Exception",
                content,
                Color.Red
            );
#endif
            return Task.CompletedTask;
        }

        if (!_settings.ShouldLogDiscordInternals)
            return Task.CompletedTask;

        switch (message)
        {
            case { Severity: LogSeverity.Warning }:
                _logger.Warning("{0}: {1}", message.Source, message.Message);
                break;
            case { Severity: LogSeverity.Debug }:
                _logger.Debug("{0}: {1}", message.Source, message.Message);
                break;
            case { Severity: LogSeverity.Info }:
                if (_messagesToBeConsideredDebug.Any(m => m.Equals(message.Message, StringComparison.Ordinal)))
                    _logger.Debug("{0}: {1}", message.Source, message.Message);
                else
                    _logger.Information("{0}: {1}", message.Source, message.Message);
                break;
            case { Severity: LogSeverity.Verbose }:
                _logger.Debug("{0}: {1}", message.Source, message.Message);
                break;
            case { Severity: LogSeverity.Error | LogSeverity.Critical }:
                _logger.Error("{0}: {1}", message.Source, message.Message);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return Task.CompletedTask;
    }
}
