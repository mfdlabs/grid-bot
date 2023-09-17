namespace Grid.Bot.Events;

using System;
using System.Threading.Tasks;

using Discord;

using Logging;

/// <summary>
/// Event invoked when Discord.Net creates a log message.
/// </summary>
public static class OnLogMessage
{
    private static readonly ILogger _logger = new Logger(
        DiscordSettings.Singleton.DiscordLoggerName,
        DiscordSettings.Singleton.DiscordLoggerLogLevel
    );

    /// <summary>
    /// Invoke the event handler.
    /// </summary>
    /// <param name="message">The <see cref="LogMessage"/></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException">The <see cref="LogSeverity"/> is out of range.</exception>
    public static Task Invoke(LogMessage message)
    {
        if (message.Exception != null)
        {
#if !DEBUG_LOG_WEBSOCKET_CLOSED_EXCEPTIONS
            if (message.Exception?.InnerException is WebSocketClosedException)
                return Task.CompletedTask;
#endif

#if DEBUG || DEBUG_LOGGING_IN_PROD
            if (!(message.Exception is TaskCanceledException &&
                  !DiscordSettings.Singleton.DebugAllowTaskCanceledExceptions))
                _logger.Error("Source = {0}, Message = {1}, Exception = {2}",
                    message.Source,
                    message.Message,
                    message.Exception.ToString()
                );
#endif
            return Task.CompletedTask;
        }

        if (!DiscordSettings.Singleton.ShouldLogDiscordInternals)
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
