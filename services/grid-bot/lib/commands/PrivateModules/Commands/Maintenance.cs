namespace Grid.Bot.Commands.Private;

using System;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using Discord.Commands;

using Extensions;

/// <summary>
/// Interaction handler for the maintenance commands.
/// </summary>
/// <remarks>
/// Construct a new instance of <see cref="Maintenance"/>.
/// </remarks>
/// <param name="maintenanceSettings">The <see cref="MaintenanceSettings"/>.</param>
/// <param name="discordSettings">The <see cref="DiscordSettings"/>.</param>
/// <param name="discordShardedClient">The <see cref="DiscordShardedClient"/>.</param>
/// <exception cref="ArgumentNullException">
/// - <paramref name="maintenanceSettings"/> cannot be null.
/// - <paramref name="discordSettings"/> cannot be null.
/// - <paramref name="discordShardedClient"/> cannot be null.
/// </exception>
[LockDownCommand(BotRole.Administrator)]
[RequireBotRole(BotRole.Administrator)]
[Group("maintenance"), Summary("Commands used for enabling and disabling maintenance mode."), Alias("maint", "m")]
public class Maintenance(
    MaintenanceSettings maintenanceSettings,
    DiscordSettings discordSettings,
    DiscordShardedClient discordShardedClient
) : ModuleBase
{
    private readonly MaintenanceSettings _maintenanceSettings = maintenanceSettings ?? throw new ArgumentNullException(nameof(maintenanceSettings));
    private readonly DiscordSettings _discordSettings = discordSettings ?? throw new ArgumentNullException(nameof(discordSettings));
    private readonly DiscordShardedClient _discordShardedClient = discordShardedClient ?? throw new ArgumentNullException(nameof(discordShardedClient));

    private static string GetStatusText(string updateText)
        => string.IsNullOrEmpty(updateText) ? "Maintenance is enabled" : $"Maintenance is enabled: {updateText}";

    /// <summary>
    /// Enables maintenance mode.
    /// </summary>
    /// <param name="statusText">The status text to set.</param>
    [Command("enable"), Summary("Enables maintenance mode.")]
    public async Task EnableMaintenanceAsync(
        [Summary("The status text to set."), Remainder]
        string statusText = null
    )
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

        _maintenanceSettings.MaintenanceEnabled = true;

        _discordShardedClient.SetStatusAsync(UserStatus.DoNotDisturb);
        _discordShardedClient.SetGameAsync(GetStatusText(statusText));

        if (!string.IsNullOrEmpty(statusText) && !_maintenanceSettings.MaintenanceStatus.Equals(statusText, StringComparison.InvariantCulture))
            _maintenanceSettings.MaintenanceStatus = statusText;

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

        _maintenanceSettings.MaintenanceEnabled = false;

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
    public async Task UpdateMaintenanceStatusTextAsync(
        [Summary("The status text to set."), Remainder]
        string statusText = null
    )
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

        _maintenanceSettings.MaintenanceStatus = statusText;

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
