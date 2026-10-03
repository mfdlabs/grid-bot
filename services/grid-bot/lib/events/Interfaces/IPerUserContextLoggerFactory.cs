namespace Grid.Bot.Events;

using Discord.WebSocket;

using Logging;

/// <summary>
/// Factory for creating loggers for users.
/// </summary>
public interface IPerUserContextLoggerFactory
{
    /// <summary>
    /// Create a logger for the specified interaction.
    /// </summary>
    /// <param name="interaction">The interaction.</param>
    /// <returns>A logger for the specified interaction.</returns>
    ILogger CreateLogger(SocketInteraction interaction);

    /// <summary>
    /// Create a logger for the specified message.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <returns>A logger for the specified message.</returns>
    ILogger CreateLogger(SocketMessage message);
}
