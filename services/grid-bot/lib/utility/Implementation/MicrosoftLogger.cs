namespace Grid.Bot.Utility;

using System;

using ILogger = Logging.ILogger;

using IMLogger = Microsoft.Extensions.Logging.ILogger;
using MEventId = Microsoft.Extensions.Logging.EventId;
using MLogLevel = Microsoft.Extensions.Logging.LogLevel;

/// <summary>
/// Implementation of <see cref="IMLogger"/> that forwards to our <see cref="ILogger"/>!
/// </summary>
/// <param name="logger">Our <see cref="ILogger"/></param>
/// <exception cref="ArgumentNullException"><paramref name="logger"/> cannot be null.</exception>
public class MicrosoftLogger(ILogger logger) : IMLogger
{
    private class NoopDisposable : IDisposable
    {
        public void Dispose() { }
    }

    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc cref="IMLogger.BeginScope{TState}(TState)"/>
    public IDisposable BeginScope<TState>(TState state) => new NoopDisposable();

    /// <inheritdoc cref="IMLogger.IsEnabled(MLogLevel)"/>
    public bool IsEnabled(MLogLevel logLevel) => true;

    /// <inheritdoc cref="IMLogger.Log{TState}(MLogLevel, MEventId, TState, Exception, Func{TState, Exception, string})"/>
    public void Log<TState>(MLogLevel logLevel, MEventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        var message = formatter(state, exception);

        switch (logLevel)
        {
            case MLogLevel.Trace:
                _logger.Verbose(message);

                break;
            case MLogLevel.Debug:
                _logger.Debug(message);

                break;
            case MLogLevel.Information:
                _logger.Information(message);

                break;
            case MLogLevel.Warning:
                _logger.Warning(message);

                break;
            case MLogLevel.Error:
            case MLogLevel.Critical:
                _logger.Error(message);

                break;
            default:
                _logger.Warning(message);

                break;
        }
    }
}