namespace Grid.Bot.Utility;

using Discord.WebSocket;

using Logging;

/// <summary>
/// Implementation of <see cref="ILoggerFactory"/>.
/// </summary>
/// <seealso cref="ILoggerFactory"/>
/// <seealso cref="ILogger"/>
public class LoggerFactory : ILoggerFactory
{
    /// <inheritdoc cref="ILoggerFactory.CreateLogger(SocketInteraction)"/>
    public ILogger CreateLogger(SocketInteraction interaction)
    {
        var name = interaction.User.Username;
        var logger = new Logger(
            name: interaction.User.Username,
            logLevel: LogLevel.Debug,
            logToFileSystem: false
        );

        logger.CustomLogPrefixes.Add(() => interaction.ChannelId.ToString());
        logger.CustomLogPrefixes.Add(() => interaction.User.Id.ToString());

        // Add guild id if the interaction is from a guild.
        if (interaction.Channel is SocketGuildChannel guildChannel)
            logger.CustomLogPrefixes.Add(() => guildChannel.Guild.Id.ToString());

        return logger;
    }

    /// <inheritdoc cref="ILoggerFactory.CreateLogger(SocketMessage)"/>
    public ILogger CreateLogger(SocketMessage message)
    {
        var name = message.Author.Username;
        var logger = new Logger(
            name: message.Author.Username,
            logLevel: LogLevel.Debug,
            logToFileSystem: false
        );

        logger.CustomLogPrefixes.Add(() => message.Channel.Id.ToString());
        logger.CustomLogPrefixes.Add(() => message.Author.Id.ToString());

        // Add guild id if the message is from a guild.
        if (message.Channel is SocketGuildChannel guildChannel)
            logger.CustomLogPrefixes.Add(() => guildChannel.Guild.Id.ToString());

        return logger;
    }
}
