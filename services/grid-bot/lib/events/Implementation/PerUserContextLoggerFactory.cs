namespace Grid.Bot.Events;

using System;

using Discord.WebSocket;

using Logging;

using Extensions;

/// <summary>
/// Implementation of <see cref="IPerUserContextLoggerFactory"/>.
/// </summary>
/// <param name="discordClient">The <see cref="DiscordShardedClient"/>.</param>
/// <exception cref="ArgumentNullException"><paramref name="discordClient"/> cannot be null.</exception>
/// <seealso cref="IPerUserContextLoggerFactory"/>
/// <seealso cref="ILogger"/>
public class PerUserContextLoggerFactory(DiscordShardedClient discordClient) : IPerUserContextLoggerFactory
{
    private readonly DiscordShardedClient _discordClient = discordClient ?? throw new ArgumentNullException(nameof(discordClient));

    /// <inheritdoc cref="IPerUserContextLoggerFactory.CreateLogger(SocketInteraction)"/>
    public ILogger CreateLogger(SocketInteraction interaction)
    {
        var name = interaction.User.Username;
        var logger = new Logger(
            name: interaction.User.Id.ToString(),
            logLevelGetter: () => LogLevel.Debug,
            logToFileSystem: false
        );

        logger.CustomLogPrefixes.Add(() => interaction.GetChannelAsString());
        logger.CustomLogPrefixes.Add(() => interaction.User.ToString());

        var guild = interaction.GetGuild(_discordClient);

        // Add guild id if the interaction is from a guild.
        if (guild is not null)
            logger.CustomLogPrefixes.Add(() => guild.ToString());

        return logger;
    }

    /// <inheritdoc cref="IPerUserContextLoggerFactory.CreateLogger(SocketMessage)"/>
    public ILogger CreateLogger(SocketMessage message)
    {
        var name = message.Author.Username;
        var logger = new Logger(
            name: message.Author.Id.ToString(),
            logLevelGetter: () => LogLevel.Debug,
            logToFileSystem: false
        );

        logger.CustomLogPrefixes.Add(() => message.Channel.ToString());
        logger.CustomLogPrefixes.Add(() => message.Author.ToString());

        // Add guild id if the message is from a guild.
        if (message.Channel is SocketGuildChannel guildChannel)
            logger.CustomLogPrefixes.Add(() => guildChannel.Guild.ToString());

        return logger;
    }
}
