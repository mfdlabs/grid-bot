namespace Grid.Bot.Events;

using System;
using System.Threading;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using Microsoft.Extensions.Options;

using Logging;

/// <summary>
/// Event handler to be invoked when a shard is ready,
/// </summary>
/// <remarks>
/// Construct a new instance of <see cref="OnShardReady"/>.
/// </remarks>
/// <param name="options">The <see cref="RecoveryOptions"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
/// <param name="client">The <see cref="DiscordShardedClient"/>.</param>
/// <param name="onMessageEvent">The <see cref="OnMessage"/>.</param>
/// <param name="onInteractionEvent">The <see cref="OnInteraction"/>.</param>
/// <exception cref="ArgumentNullException">
/// - <paramref name="options"/> cannot be null.
/// - <paramref name="logger"/> cannot be null.
/// - <paramref name="client"/> cannot be null.
/// - <paramref name="onMessageEvent"/> cannot be null.
/// - <paramref name="onInteractionEvent"/> cannot be null.
/// </exception>
public class OnShardReady(
    IOptions<RecoveryOptions> options,
    ILogger logger,
    DiscordShardedClient client,
    OnMessage onMessageEvent,
    OnInteraction onInteractionEvent
)
{
    private int _readyFlag = 0;

    private readonly RecoveryOptions _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
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
        _logger.Debug(
            "Shard '{0}' ready as '{0}#{1}'",
            shard.ShardId,
            _client.CurrentUser.Username,
            _client.CurrentUser.Discriminator
        );


        if (Interlocked.Exchange(ref _readyFlag, 1) == 0)
        {
            _client.MessageReceived += _onMessageEvent.Invoke;
            _client.InteractionCreated += _onInteractionEvent.Invoke;

            var text = _settings.MaintenanceStatusMessage;

            _client.SetStatusAsync(UserStatus.DoNotDisturb);
            _client.SetGameAsync(GetStatusText(text));
        }


        return Task.CompletedTask;
    }
}
