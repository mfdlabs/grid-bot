namespace Grid.Bot.Events;

using System;
using System.Threading;
using System.Reflection;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using Discord.Commands;
using Discord.Interactions;

using Microsoft.Extensions.Options;

using Logging;

/// <summary>
/// Event handler to be invoked when a shard is ready,
/// </summary>
/// <remarks>
/// Construct a new instance of <see cref="OnShardReady"/>.
/// </remarks>
/// <param name="discordOptions">The <see cref="DiscordOptions"/>.</param>
/// <param name="maintenanceSettings">The <see cref="MaintenanceSettings"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
/// <param name="client">The <see cref="DiscordShardedClient"/>.</param>
/// <param name="interactionService">The <see cref="InteractionService"/>.</param>
/// <param name="commandService">The <see cref="CommandService"/>.</param>
/// <param name="services">The <see cref="IServiceProvider"/>.</param>
/// <param name="onMessageEvent">The <see cref="OnMessage"/>.</param>
/// <param name="onInteractionEvent">The <see cref="OnInteraction"/>.</param>
/// <param name="onInteractionExecutedEvent">The <see cref="OnInteractionExecuted"/>.</param>
/// <param name="onCommandExecutedEvent">The <see cref="OnCommandExecuted"/>.</param>
/// <exception cref="ArgumentNullException">
/// - <paramref name="discordOptions"/> cannot be null.
/// - <paramref name="maintenanceSettings"/> cannot be null.
/// - <paramref name="logger"/> cannot be null.
/// - <paramref name="client"/> cannot be null.
/// - <paramref name="interactionService"/> cannot be null.
/// - <paramref name="commandService"/> cannot be null.
/// - <paramref name="services"/> cannot be null.
/// - <paramref name="onMessageEvent"/> cannot be null.
/// - <paramref name="onInteractionEvent"/> cannot be null.
/// - <paramref name="onInteractionExecutedEvent"/> cannot be null.
/// - <paramref name="onCommandExecutedEvent"/> cannot be null.	
/// </exception>
public class OnShardReady(
    IOptionsMonitor<DiscordOptions> discordOptions,
    MaintenanceSettings maintenanceSettings,
    ILogger logger,
    DiscordShardedClient client,
    InteractionService interactionService,
    CommandService commandService,
    IServiceProvider services,
    OnMessage onMessageEvent,
    OnInteraction onInteractionEvent,
    OnInteractionExecuted onInteractionExecutedEvent,
    OnCommandExecuted onCommandExecutedEvent
)
{
    private static readonly Assembly _commandsAssembly = Assembly.Load("Grid.Bot.Commands");

    private int _shardCount = 0;

    private readonly IOptionsMonitor<DiscordOptions> _discordOptions = discordOptions ?? throw new ArgumentNullException(nameof(discordOptions));

    private DiscordOptions _discordSettings => _discordOptions.CurrentValue;
    private readonly MaintenanceSettings _maintenanceSettings = maintenanceSettings ?? throw new ArgumentNullException(nameof(maintenanceSettings));

    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly DiscordShardedClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly InteractionService _interactionService = interactionService ?? throw new ArgumentNullException(nameof(interactionService));
    private readonly CommandService _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
    private readonly IServiceProvider _services = services ?? throw new ArgumentNullException(nameof(services));

    private readonly OnMessage _onMessageEvent = onMessageEvent ?? throw new ArgumentNullException(nameof(onMessageEvent));
    private readonly OnInteraction _onInteractionEvent = onInteractionEvent ?? throw new ArgumentNullException(nameof(onInteractionEvent));
    private readonly OnInteractionExecuted _onInteractionExecutedEvent = onInteractionExecutedEvent ?? throw new ArgumentNullException(nameof(onInteractionExecutedEvent));
    private readonly OnCommandExecuted _onCommandExecutedEvent = onCommandExecutedEvent ?? throw new ArgumentNullException(nameof(onCommandExecutedEvent));

    private static string GetStatusText(string updateText)
        => string.IsNullOrEmpty(updateText) ? "Maintenance is enabled" : $"Maintenance is enabled: {updateText}";

    /// <summary>
    /// Invoe the event handler.
    /// </summary>
    /// <param name="shard">The client for the shard.</param>
    public async Task Invoke(DiscordSocketClient shard)
    {
        Interlocked.Increment(ref _shardCount);

        _logger.Debug(
            "Shard '{0}' ready as '{0}#{1}'",
            shard.ShardId,
            _client.CurrentUser.Username,
            _client.CurrentUser.Discriminator
        );

        if (_shardCount == _client.Shards.Count)
        {
            await _interactionService.AddModulesAsync(_commandsAssembly, _services);
            await _commandService.AddModulesAsync(_commandsAssembly, _services);

#if DEBUG
            if (_discordSettings.DebugGuildId != 0)
                await _interactionService.RegisterCommandsToGuildAsync(_discordSettings.DebugGuildId);
            else
                await _interactionService.RegisterCommandsGloballyAsync();
#else
            await _interactionService.RegisterCommandsGloballyAsync();
#endif

            _onMessageEvent.Initialize();

            _client.MessageReceived += _onMessageEvent.Invoke;
            _client.InteractionCreated += _onInteractionEvent.Invoke;

            _interactionService.InteractionExecuted += _onInteractionExecutedEvent.Invoke;
            _commandService.CommandExecuted += _onCommandExecutedEvent.Invoke;

            if (_maintenanceSettings.MaintenanceEnabled)
            {
                var text = _maintenanceSettings.MaintenanceStatus;

                _client.SetStatusAsync(UserStatus.DoNotDisturb);
                _client.SetGameAsync(GetStatusText(text));

                return;
            }

            _client.SetStatusAsync(_discordSettings.BotStatus);

            if (!string.IsNullOrEmpty(_discordSettings.BotStatusMessage))
                _client.SetGameAsync(
                    _discordSettings.BotStatusMessage
                );
        }
    }
}
