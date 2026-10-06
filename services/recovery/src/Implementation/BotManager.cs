namespace Grid.Bot;

using System;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using Microsoft.Extensions.Options;

using Logging;

using Events;

/// <summary>
/// Manager for the bot.
/// </summary>
public class BotManager : IBotManager
{
    private readonly RecoveryOptions _settings;
    private readonly ILogger _logger;
    private readonly DiscordShardedClient _client;

    private readonly OnShardReady _onReady;
    private readonly OnLogMessage _onLogMessage;

    /// <summary>
    /// Construct a new instance of <see cref="BotManager"/>.
    /// </summary>
    /// <param name="options">The <see cref="RecoveryOptions"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="client">The <see cref="DiscordShardedClient"/>.</param>
    /// <param name="onReady">The <see cref="OnShardReady"/>.</param>
    /// <param name="onLogMessage">The <see cref="OnLogMessage"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="options"/> cannot be null.
    /// - <paramref name="client"/> cannot be null.
    /// - <paramref name="onReady"/> cannot be null.
    /// - <paramref name="onLogMessage"/> cannot be null.
    /// </exception>
    public BotManager(
        IOptions<RecoveryOptions> options,
        ILogger logger,
        DiscordShardedClient client,
        OnShardReady onReady,
        OnLogMessage onLogMessage
    )
    {
        _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        try
        {
            await _client.LoginAsync(TokenType.Bot, _settings.BotToken);
            await _client.StartAsync();
        }
        catch (Exception ex)
        {
            _logger.Error("Error starting bot: {0}", ex.Message);
        }
    }

    /// <summary>
    /// Stop the bot.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task StopAsync()
    {
        try
        {
            await _client.StopAsync();
            await _client.LogoutAsync();
        }
        catch (Exception ex)
        {
            _logger.Error("Error stopping bot: {0}", ex.Message);
        }
    }
}
