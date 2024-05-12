namespace Grid.Bot;

using System;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using Events;

/// <summary>
/// Manager for the bot.
/// </summary>
public class BotManager : IBotManager
{
    private readonly ISettings _settings;
    private readonly DiscordShardedClient _client;
    
    private readonly OnShardReady _onReady;
    private readonly OnLogMessage _onLogMessage;

    /// <summary>
    /// Construct a new instance of <see cref="BotManager"/>.
    /// </summary>
    /// <param name="settings">The <see cref="ISettings"/>.</param>
    /// <param name="client">The <see cref="DiscordShardedClient"/>.</param>
    /// <param name="onReady">The <see cref="OnShardReady"/>.</param>
    /// <param name="onLogMessage">The <see cref="OnLogMessage"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="settings"/> cannot be null.
    /// - <paramref name="client"/> cannot be null.
    /// - <paramref name="onReady"/> cannot be null.
    /// - <paramref name="onLogMessage"/> cannot be null.
    /// </exception>
    public BotManager(
        ISettings settings,
        DiscordShardedClient client,
        OnShardReady onReady,
        OnLogMessage onLogMessage
    )
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _onReady = onReady ?? throw new ArgumentNullException(nameof(onReady));
        _onLogMessage = onLogMessage ?? throw new ArgumentNullException(nameof(onLogMessage));

        _client.ShardReady += _onReady.Invoke;
        _client.Log += _onLogMessage.Invoke;
    }

    /// <summary>
    /// Start the bot.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task StartAsync()
    {
        await _client.LoginAsync(TokenType.Bot, _settings.BotToken);
        await _client.StartAsync();
    }

    /// <summary>
    /// Stop the bot.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task StopAsync()
    {
        await _client.StopAsync();
        await _client.LogoutAsync();
    }
}
