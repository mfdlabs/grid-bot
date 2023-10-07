namespace Grid.Bot.Events;

using System;
using System.Threading.Tasks;

using Discord;

using Logging;

/// <summary>
/// Event invoked when Discord.Net creates a log message.
/// </summary>
public class OnLogMessage
{
    private readonly DiscordSettings _settings;
    private readonly ILogger _logger;

    /// <summary>
    /// Construct a new instance of <see cref="OnLogMessage"/>.
    /// </summary>
    /// <param name="settings">The <see cref="DiscordSettings"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> cannot be null.</exception>
    public OnLogMessage(DiscordSettings settings)
    {
        if (settings is null)
            throw new ArgumentNullException(nameof(settings));

        _settings = settings;

        _logger = new Logger(
            name: _settings.DiscordLoggerName,
            logLevel: _settings.DiscordLoggerLogLevel,
            logToConsole: _settings.DiscordLoggerLogToConsole
        );
    }

    /// <summary>
    /// Invoke the event handler.
    /// </summary>
    /// <param name="message">The <see cref="LogMessage"/></param>
    public Task Invoke(LogMessage message)
    {
        if (message.Exception != null)
        {
#if !DEBUG_LOG_WEBSOCKET_CLOSED_EXCEPTIONS
            if (message.Exception?.InnerException is WebSocketClosedException)
                return Task.CompletedTask;
#endif

#if DEBUG || DEBUG_LOGGING_IN_PROD
            if (!(message.Exception is TaskCanceledException &&
                  !_settings.DebugAllowTaskCanceledExceptions))
                _logger.Error("Source = {0}, Message = {1}, Exception = {2}",
                    message.Source,
                    message.Message,
                    message.Exception.ToString()
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
