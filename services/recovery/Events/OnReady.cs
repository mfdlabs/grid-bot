namespace Grid.Bot.Events;

using System;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using Logging;

using Threading;

/// <summary>
/// Event handler to be invoked when a shard is ready,
/// </summary>
/// <remarks>
/// Construct a new instance of <see cref="OnShardReady"/>.
/// </remarks>
/// <param name="settings">The <see cref="ISettings"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
/// <param name="client">The <see cref="DiscordShardedClient"/>.</param>
/// <param name="onMessageEvent">The <see cref="OnMessage"/>.</param>
/// <param name="onInteractionEvent">The <see cref="OnInteraction"/>.</param>
/// <exception cref="ArgumentNullException">
/// - <paramref name="settings"/> cannot be null.
/// - <paramref name="logger"/> cannot be null.
/// - <paramref name="client"/> cannot be null.
/// - <paramref name="onMessageEvent"/> cannot be null.
/// - <paramref name="onInteractionEvent"/> cannot be null.
/// </exception>
public class OnShardReady(
    ISettings settings,
    ILogger logger,
    DiscordShardedClient client,
    OnMessage onMessageEvent,
    OnInteraction onInteractionEvent
)
{
    private Atomic<int> _shardCount = 0; // needs to be atomic due to the race situation here.

    private readonly ISettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly DiscordShardedClient _client = client ?? throw new ArgumentNullException(nameof(client));

    private readonly OnMessage _onMessageEvent = onMessageEvent ?? throw new ArgumentNullException(nameof(onMessageEvent));
    private readonly OnInteraction _onInteractionEvent = onInteractionEvent ?? throw new ArgumentNullException(nameof(onInteractionEvent));

    private static string GetStatusText(string updateText)
        => string.IsNullOrEmpty(updateText) ? "Maintenance is enabled" : $"Maintenance is enabled: {updateText}";

    /// <summary>
    /// Invoe the event handler.
    /// </summary>
    /// <param name="shard">The client for the shard.</param>
    public Task Invoke(DiscordSocketClient shard)
    {
        _shardCount++;

        _logger.Debug(
            "Shard '{0}' ready as '{0}#{1}'",
            shard.ShardId,
            _client.CurrentUser.Username,
            _client.CurrentUser.Discriminator
        );

        if (_shardCount == _client.Shards.Count)
        {
            _shardCount = 0;

            _logger.Debug("Final shard ready!");

            _client.MessageReceived += _onMessageEvent.Invoke;
            _client.InteractionCreated += _onInteractionEvent.Invoke;

            var text = _settings.MaintenanceStatusMessage;

            _client.SetStatusAsync(UserStatus.DoNotDisturb);
            _client.SetGameAsync(GetStatusText(text));

        }

        return Task.CompletedTask;
    }
}
