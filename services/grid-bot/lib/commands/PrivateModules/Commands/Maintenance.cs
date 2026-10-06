namespace Grid.Bot.Commands.Private;

using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.WebSocket;

using Discord.Commands;

using Microsoft.Extensions.Options;

using Utility;

/// <summary>
/// Command handler for the maintenance commands.
/// </summary>
/// <remarks>
/// Construct a new instance of <see cref="Maintenance"/>.
/// </remarks>
/// <param name="maintenanceOptions">The <see cref="MaintenanceOptions"/>.</param>
/// <param name="discordOptions">The <see cref="DiscordOptions"/>.</param>
/// <param name="discordShardedClient">The <see cref="DiscordShardedClient"/>.</param>
/// <param name="settingsWriter">The <see cref="ISettingsWriter"/>.</param>
/// <exception cref="ArgumentNullException">
/// - <paramref name="maintenanceOptions"/> cannot be null.
/// - <paramref name="discordOptions"/> cannot be null.
/// - <paramref name="discordShardedClient"/> cannot be null.
/// - <paramref name="settingsWriter"/> cannot be null.
/// </exception>
[LockDownCommand(BotRole.Administrator)]
[RequireBotRole(BotRole.Administrator)]
[Group("maintenance"), Summary("Commands used for enabling and disabling maintenance mode."), Alias("maint", "m")]
public class Maintenance(
    IOptionsMonitor<MaintenanceOptions> maintenanceOptions,
    IOptionsMonitor<DiscordOptions> discordOptions,
    DiscordShardedClient discordShardedClient,
    ISettingsWriter settingsWriter
) : ModuleBase
{
    private readonly IOptionsMonitor<MaintenanceOptions> _maintenanceOptions = maintenanceOptions ?? throw new ArgumentNullException(nameof(maintenanceOptions));
    private readonly ISettingsWriter _settingsWriter = settingsWriter ?? throw new ArgumentNullException(nameof(settingsWriter));

    private MaintenanceOptions _maintenanceSettings => _maintenanceOptions.CurrentValue;
    private readonly IOptionsMonitor<DiscordOptions> _discordOptions = discordOptions ?? throw new ArgumentNullException(nameof(discordOptions));

    private DiscordOptions _discordSettings => _discordOptions.CurrentValue;
    private readonly DiscordShardedClient _discordShardedClient = discordShardedClient ?? throw new ArgumentNullException(nameof(discordShardedClient));

    private static string GetStatusText(string updateText)
        => string.IsNullOrEmpty(updateText) ? "Maintenance is enabled" : $"Maintenance is enabled: {updateText}";

    /// <summary>
    /// Enables maintenance mode.
    /// </summary>
    /// <param name="statusText">The status text to set.</param>
    [Command("enable"), Summary("Enables maintenance mode.")]
    public async Task EnableMaintenanceAsync([Remainder] string statusText = null)
    {
        if (_maintenanceSettings.MaintenanceEnabled)
        {
            if (!string.IsNullOrEmpty(statusText) && !_maintenanceSettings.MaintenanceStatus.Equals(statusText, StringComparison.InvariantCulture))
            {
                await this.ReplyWithReferenceAsync("The maintenance status is already enabled, and it appears you have a different message.");

                return;
            }

            await this.ReplyWithReferenceAsync("Maintenance is already enabled.");

            return;
        }

        if (string.IsNullOrEmpty(statusText))
            statusText = _maintenanceSettings.MaintenanceStatus;

        var values = new Dictionary<string, string>
        {
            [nameof(MaintenanceOptions.MaintenanceEnabled)] = "true"
        };

        if (!string.IsNullOrEmpty(statusText) && !_maintenanceSettings.MaintenanceStatus.Equals(statusText, StringComparison.InvariantCulture))
            values[nameof(MaintenanceOptions.MaintenanceStatus)] = statusText;

        await _settingsWriter.SetAsync(MaintenanceOptions.SectionName, values);

        _discordShardedClient.SetStatusAsync(UserStatus.DoNotDisturb);
        _discordShardedClient.SetGameAsync(GetStatusText(statusText));

        await this.ReplyWithReferenceAsync($"Successfully enabled the maintenance status with the optional message of '{(string.IsNullOrEmpty(statusText) ? "No Message" : statusText)}'!");
    }

    /// <summary>
    /// Disables maintenance mode.
    /// </summary>
    [Command("disable"), Summary("Disables maintenance mode.")]
    public async Task DisableMaintenanceAsync()
    {
        if (!_maintenanceSettings.MaintenanceEnabled)
        {
            await this.ReplyWithReferenceAsync("The maintenance status is not enabled!");

            return;
        }

        await _settingsWriter.SetAsync(MaintenanceOptions.SectionName, nameof(MaintenanceOptions.MaintenanceEnabled), "false");

        _discordShardedClient.SetStatusAsync(_discordSettings.BotStatus);

        if (!string.IsNullOrEmpty(_discordSettings.BotStatusMessage))
            _discordShardedClient.SetGameAsync(_discordSettings.BotStatusMessage);

        await this.ReplyWithReferenceAsync("Successfully disabled the maintenance status!");
    }

    /// <summary>
    /// Updates the maintenance status text.
    /// </summary>
    /// <param name="statusText">The status text to set.</param>
    [Command("update"), Summary("Updates the maintenance status text.")]
    public async Task UpdateMaintenanceStatusTextAsync([Remainder] string statusText = null)
    {
        if (!_maintenanceSettings.MaintenanceEnabled)
        {
            await this.ReplyWithReferenceAsync("The maintenance status is not enabled!");

            return;
        }

        var oldMessage = _maintenanceSettings.MaintenanceStatus;

        if (string.IsNullOrEmpty(statusText)) statusText = string.Empty;

        if (oldMessage.Equals(statusText, StringComparison.InvariantCulture))
        {
            await this.ReplyWithReferenceAsync("No changes were made to the maintenance status text, therefore no update was made.");

            return;
        }

        await _settingsWriter.SetAsync(MaintenanceOptions.SectionName, nameof(MaintenanceOptions.MaintenanceStatus), statusText);

        _discordShardedClient.SetGameAsync(GetStatusText(statusText));

        await this.ReplyWithReferenceAsync($"Successfully updated the maintenance status text to '{statusText}'!");
    }

    /// <summary>
    /// Gets the maintenance status.
    /// </summary>
    [Command("status"), Summary("Gets the maintenance status.")]
    [Alias("get")]
    public async Task GetMaintenanceStatusAsync()
    {
        var embed = new EmbedBuilder()
            .WithTitle("Maintenance Status")
            .WithDescription(_maintenanceSettings.MaintenanceEnabled ? "Maintenance is enabled." : "Maintenance is disabled.")
            .WithColor(_maintenanceSettings.MaintenanceEnabled ? Color.Red : Color.Green)
            .WithCurrentTimestamp()
            .AddField("Maintenance Status", _maintenanceSettings.MaintenanceEnabled ? "Enabled" : "Disabled")
            .AddField("Maintenance Status Text", string.IsNullOrEmpty(_maintenanceSettings.MaintenanceStatus) ? "No Message" : _maintenanceSettings.MaintenanceStatus)
            .Build();

        await this.ReplyWithReferenceAsync(embed: embed);
    }
}
