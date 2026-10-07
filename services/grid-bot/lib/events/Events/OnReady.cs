namespace Grid.Bot.Events;

using System;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.WebSocket;

using Discord.Commands;
using Discord.Interactions;

using Microsoft.Extensions.Options;

using Logging;

using Utility;

/// <summary>
/// Event handler to be invoked when a shard is ready,
/// </summary>
/// <remarks>
/// Construct a new instance of <see cref="OnShardReady"/>.
/// </remarks>
/// <param name="discordOptions">The <see cref="DiscordOptions"/>.</param>
/// <param name="maintenanceOptions">The <see cref="MaintenanceOptions"/>.</param>
/// <param name="discordWebhookAlertManager">The <see cref="IDiscordWebhookAlertManager"/>.</param>
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
/// - <paramref name="maintenanceOptions"/> cannot be null.
/// - <paramref name="discordWebhookAlertManager"/> cannot be null.
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
    IOptionsMonitor<MaintenanceOptions> maintenanceOptions,
    IDiscordWebhookAlertManager discordWebhookAlertManager,
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
    private Stopwatch _startupSw;

    private readonly IOptionsMonitor<DiscordOptions> _discordOptions = discordOptions ?? throw new ArgumentNullException(nameof(discordOptions));

    private DiscordOptions _discordSettings => _discordOptions.CurrentValue;
    private readonly IOptionsMonitor<MaintenanceOptions> _maintenanceOptions = maintenanceOptions ?? throw new ArgumentNullException(nameof(maintenanceOptions));

    private MaintenanceOptions _maintenanceSettings => _maintenanceOptions.CurrentValue;

    private readonly IDiscordWebhookAlertManager _discordWebhookAlertManager = discordWebhookAlertManager ?? throw new ArgumentNullException(nameof(discordWebhookAlertManager));
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

    private static string FormatTimeSpan(TimeSpan span)
    {
        var parts = new List<string>();
        if (span.Minutes > 0) parts.Add($"{span.Minutes}min");
        if (span.Seconds > 0) parts.Add($"{span.Seconds}s");
        if (span.Milliseconds > 0) parts.Add($"{span.Milliseconds}ms");
        return string.Join(" ", parts);
    }

    /// <summary>
    /// Invoe the event handler.
    /// </summary>
    /// <param name="shard">The client for the shard.</param>
    public async Task Invoke(DiscordSocketClient shard)
    {
        Interlocked.Increment(ref _shardCount);

        if (_shardCount == 1 && _startupSw == null)
        {
            _startupSw = Stopwatch.StartNew();

            if (_discordSettings.AlertLogStartups)
                await _discordWebhookAlertManager.SendAlertAsync(
                    topic: $"Startup ({EnvironmentDataProvider.EnvironmentName})",
                    message: $"Start up has begun for '{_client.CurrentUser}' with {_client.Shards.Count} shards...",
                    color: Color.LightOrange
                ).ConfigureAwait(false);
        }

        _logger.Debug(
            "Shard '{0}' ready as '{0}'",
            shard.ShardId,
            _client.CurrentUser.ToString()
        );

        if (_shardCount == _client.Shards.Count)
        {
            _startupSw.Stop();

            if (_discordSettings.AlertLogFinalShardReady)
                await _discordWebhookAlertManager.SendAlertAsync(
                    topic: $"Startup ({EnvironmentDataProvider.EnvironmentName})",
                    message: $"Final shard for '{_client.CurrentUser}' is ready, took {FormatTimeSpan(_startupSw.Elapsed)} to startup {_client.Shards.Count} shards!",
                    color: Color.Green
                ).ConfigureAwait(false);

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
