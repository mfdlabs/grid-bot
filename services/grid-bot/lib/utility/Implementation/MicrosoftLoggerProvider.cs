namespace Grid.Bot.Utility;

using System;

using ILogger = Logging.ILogger;

using IMLogger = Microsoft.Extensions.Logging.ILogger;
using IMLoggerProvider = Microsoft.Extensions.Logging.ILoggerProvider;

/// <summary>
/// Provider for the <see cref="MicrosoftLogger"/> class.
/// </summary>
/// <param name="logger">Our <see cref="ILogger" /></param>
/// <exception cref="ArgumentNullException"><paramref name="logger"/> cannot be null.</exception>
public class MicrosoftLoggerProvider(ILogger logger) : IMLoggerProvider
{
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc cref="IMLoggerProvider.CreateLogger(string)"/>
    public IMLogger CreateLogger(string categoryName) => new MicrosoftLogger(_logger);

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose() { }
}