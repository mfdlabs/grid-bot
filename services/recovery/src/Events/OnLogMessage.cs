namespace Grid.Bot.Events;

using System;
using System.Net.WebSockets;
using System.Threading.Tasks;

using Discord;
using Discord.Net;
using Discord.WebSocket;

using Prometheus;

using Logging;

/// <summary>
/// Event invoked when Discord.Net creates a log message.
/// </summary>
public class OnLogMessage
{
    private readonly Logger _logger = new(
        name: "discord",
        logLevel: LogLevel.Debug,
        logToConsole: true,
        logToFileSystem: false
    );

    private readonly Counter _totalLogMessages = Metrics.CreateCounter(
        "grid_discord_log_messages_total",
        "The total number of log messages.",
        "log_severity"
    );

    /// <summary>
    /// Invoke the event handler.
    /// </summary>
    /// <param name="message">The <see cref="LogMessage"/></param>
    public Task Invoke(LogMessage message)
    {
        _totalLogMessages.WithLabels(message.Severity.ToString()).Inc();

        if (message.Exception != null)
        {
            if (message.Exception is GatewayReconnectException)
                return Task.CompletedTask;

            // Closed web socket exceptions are expected when the bot is shutting down.
            if (message.Exception.InnerException is WebSocketException)
                return Task.CompletedTask;

            if (message.Exception is WebSocketClosedException || message.Exception.InnerException is WebSocketClosedException)
                return Task.CompletedTask;

            if (message.Exception is TaskCanceledException)
                return Task.CompletedTask;

            _logger.Error(
                "Source = {0}, Message = {1}, Exception = {2}",
                message.Source,
                message.Message,
                message.Exception.ToString()
            );

            return Task.CompletedTask;
        }

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
