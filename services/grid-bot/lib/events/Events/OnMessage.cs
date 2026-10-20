namespace Grid.Bot.Events;

using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

#if DEBUG
using System.Reflection;
#endif

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.Options;

using Prometheus;
using Grid.Bot.Metrics;
using Grid.Bot.Telemetry;

using Utility;

/// <summary>
/// Event handler for messages.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OnMessage"/> class.
/// </remarks>
/// <param name="commandsOptions">The <see cref="CommandsOptions"/>.</param>
/// <param name="maintenanceOptions">The <see cref="MaintenanceOptions"/>.</param>
/// <param name="adminUtility">The <see cref="IAdminUtility"/>.</param>
/// <param name="loggerFactory">The <see cref="IPerUserContextLoggerFactory"/>.</param>
/// <param name="commandService">The <see cref="CommandService"/>.</param>
/// <param name="discordClient">The <see cref="DiscordShardedClient"/>.</param>
/// <param name="services">The <see cref="IServiceProvider"/>.</param>
/// <exception cref="ArgumentNullException">
/// - <paramref name="commandsOptions"/> cannot be null.
/// - <paramref name="maintenanceOptions"/> cannot be null.
/// - <paramref name="adminUtility"/> cannot be null.
/// - <paramref name="loggerFactory"/> cannot be null.
/// - <paramref name="commandService"/> cannot be null.
/// - <paramref name="discordClient"/> cannot be null.
/// - <paramref name="services"/> cannot be null.
/// </exception>
public partial class OnMessage(
    IOptionsMonitor<CommandsOptions> commandsOptions,
    IOptionsMonitor<MaintenanceOptions> maintenanceOptions,
    IAdminUtility adminUtility,
    IPerUserContextLoggerFactory loggerFactory,
    CommandService commandService,
    DiscordShardedClient discordClient,
    IServiceProvider services
)
{
    // language=regex
    private const string _allowedCommandRegex = @"^[a-zA-Z-_]*$";

    [GeneratedRegex(_allowedCommandRegex, RegexOptions.Singleline)]
    private static partial Regex GetAllowedCommandRegex();

    private readonly IOptionsMonitor<CommandsOptions> _commandsOptions = commandsOptions ?? throw new ArgumentNullException(nameof(commandsOptions));

    private CommandsOptions _commandsSettings => _commandsOptions.CurrentValue;
    private readonly IOptionsMonitor<MaintenanceOptions> _maintenanceOptions = maintenanceOptions ?? throw new ArgumentNullException(nameof(maintenanceOptions));

    private MaintenanceOptions _maintenanceSettings => _maintenanceOptions.CurrentValue;

    private readonly IAdminUtility _adminUtility = adminUtility ?? throw new ArgumentNullException(nameof(adminUtility));
    private readonly IPerUserContextLoggerFactory _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

    private readonly CommandService _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
    private readonly DiscordShardedClient _discordClient = discordClient ?? throw new ArgumentNullException(nameof(discordClient));
    private readonly IServiceProvider _services = services ?? throw new ArgumentNullException(nameof(services));

    private readonly HashSet<string> _commandAliases = []; // Used to filter out commands that are not meant for the bot.

    private readonly Counter _totalMessagesProcessed = GridBotMetrics.Messages.Processed;

    /// <summary>
    /// Initialize the event handler.
    /// </summary>
    public void Initialize()
    {
        var modules = _commandService.Modules;

        _commandAliases.Clear();

        _commandAliases.UnionWith(
            modules.SelectMany(x => x.Aliases)
                .Where(x => !string.IsNullOrEmpty(x))
                .Select(x => x.ToLowerInvariant())
        );

        _commandAliases.UnionWith(
            modules.SelectMany(x => x.Commands)
                .SelectMany(x => x.Aliases)
                .Where(x => !string.IsNullOrEmpty(x))
                .Select(x => x.ToLowerInvariant())
                .Select(x => x.Split(' ').First())
        );

        // Create each command's series at zero, so the first execution of a command is counted by increase().
        foreach (var alias in _commandAliases)
            foreach (var outcome in MetricOutcomes.All)
                GridBotMetrics.Commands.Executions.WithLabels(alias, outcome);
    }

    private static string GetGuildId(SocketMessage message)
    {
        if (message.Channel is SocketGuildChannel guildChannel)
            return guildChannel.Guild.Id.ToString();

        return "DM";
    }

    /// <summary>
    /// Invoke the event handler.
    /// </summary>
    /// <param name="rawMessage">The <see cref="SocketMessage"/></param>
    public async Task Invoke(SocketMessage rawMessage)
    {
        if (rawMessage is not SocketUserMessage message) return;
        if (message.Author.IsBot) return;

        _totalMessagesProcessed.Inc();

        int argPos = 0;

        if (!message.HasStringPrefix(_commandsSettings.Prefix, ref argPos, StringComparison.OrdinalIgnoreCase)) return;

        // Get the name of the command that was used.
        var commandName = message.Content.Split(' ').First();
        if (string.IsNullOrEmpty(commandName)) return;

        commandName = commandName[argPos..];
        if (string.IsNullOrEmpty(commandName)) return;
        if (!GetAllowedCommandRegex().IsMatch(commandName)) return;
        if (!_commandAliases.Contains(commandName.ToLowerInvariant())) return;

        using var logger = _loggerFactory.CreateLogger(message);

        var userIsAdmin = _adminUtility.UserIsAdmin(message.Author);
        var userIsPrivilaged = _adminUtility.UserIsPrivilaged(message.Author);
        var userIsBlacklisted = _adminUtility.UserIsBlacklisted(message.Author);

        // Aliases are matched case-insensitively, normalize so each command has a single series.
        var metricCommandName = commandName.ToLowerInvariant();

#if DEBUG

        var entryAssembly = Assembly.GetEntryAssembly();
        var informationalVersion = entryAssembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (string.IsNullOrEmpty(informationalVersion))
            informationalVersion = entryAssembly?.GetName().Version?.ToString();

        if (!string.IsNullOrEmpty(informationalVersion))
            await message.Channel.SendMessageAsync($"Debug build running version {informationalVersion}.");

#endif

        if (_maintenanceSettings.MaintenanceEnabled)
        {
            if (!userIsAdmin && !userIsPrivilaged)
            {
                GridBotMetrics.Requests.Rejected.WithLabels(MetricLabelValues.Source.Message, MetricLabelValues.RejectionReason.Maintenance).Inc();
                GridBotMetrics.Commands.Executions.WithLabels(metricCommandName, MetricOutcomes.Rejected).Inc();

                logger.Warning(
                    "User tried to use the command '{0}', but maintenance is enabled.",
                    commandName
                );

                var failureMessage = _maintenanceSettings.MaintenanceStatus;

                var embed = new EmbedBuilder()
                    .WithTitle("Maintenance Enabled")
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp();

                if (!string.IsNullOrEmpty(failureMessage))
                    embed.WithDescription(failureMessage);

                await message.ReplyAsync("Maintenance is currently enabled, please try again later.", embeds: [embed.Build()]);

                return;
            }

            GridBotMetrics.Requests.MaintenanceBypasses.WithLabels(MetricLabelValues.Source.Message).Inc();
        }

        if (userIsBlacklisted)
        {
            GridBotMetrics.Requests.Rejected.WithLabels(MetricLabelValues.Source.Message, MetricLabelValues.RejectionReason.Blacklisted).Inc();
            GridBotMetrics.Commands.Executions.WithLabels(metricCommandName, MetricOutcomes.Rejected).Inc();

            logger.Warning("Blacklisted user tried to use the bot.");

            try
            {
                var dmChannel = await message.Author.CreateDMChannelAsync();

                await dmChannel?.SendMessageAsync(
                    "You are blacklisted from using the bot, please contact the bot owner for more information."
                );
            }
            catch (Exception ex)
            {
                logger.Error("Failed to send blacklisted user a DM because: {0}", ex);
            }

            return;
        }

        if (message.Content.Contains("@everyone", StringComparison.CurrentCultureIgnoreCase) ||
            message.Content.Contains("@here", StringComparison.CurrentCultureIgnoreCase) &&
            !userIsAdmin)
            return;

        if (!_commandsSettings.EnableTextCommands)
        {
            if (!_commandsSettings.ShouldWarnWhenCommandsAreDisabled)
                return;

            if (string.IsNullOrEmpty(_commandsSettings.TextCommandsDisabledWarningText))
                await message.ReplyAsync("Text commands are disabled, please use slash commands instead.");
            else
                await message.ReplyAsync(_commandsSettings.TextCommandsDisabledWarningText);

            return;
        }

        Task.Run(async () =>
        {
            using var inFlight = GridBotMetrics.Commands.InFlight.TrackInProgress();

            var stopwatch = Stopwatch.StartNew();
            var outcome = MetricOutcomes.Exception;

            using var activity = GridBotTelemetry.Source.StartActivity(GridBotTelemetry.Spans.Command);
            if (activity is not null)
            {
                activity.SetTag(GridBotTelemetry.Tags.CommandName, metricCommandName);
                activity.SetTag(GridBotTelemetry.Tags.CommandType, "text");
                activity.SetTag(GridBotTelemetry.Tags.DiscordUserId, message.Author.Id);
                activity.SetTag(GridBotTelemetry.Tags.DiscordChannelId, message.Channel?.Id);
                activity.SetTag(GridBotTelemetry.Tags.DiscordGuildId, GetGuildId(message));
            }

            try
            {
                var context = new ShardedCommandContext(_discordClient, message);
                using var __ = context.Channel.EnterTypingState();

                var result = await _commandService.ExecuteAsync(context, argPos, _services).ConfigureAwait(false);

                outcome = OutcomeClassifier.FromCommandResult(result);
            }
            finally
            {
                activity.SetOutcome(outcome);

                GridBotMetrics.Commands.Executions.WithLabels(metricCommandName, outcome).Inc();
                GridBotMetrics.Commands.Duration
                    .WithLabels(metricCommandName, outcome)
                    .Observe(stopwatch.Elapsed.TotalSeconds);
            }
        });
    }
}
