namespace Grid.Bot.Utility;

using System;
using System.Text;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

using Prometheus;

using Discord;

using Newtonsoft.Json;

using Microsoft.Extensions.Options;

/// <summary>
/// Handles sending alerts to a Discord webhook.
/// </summary>
/// <seealso cref="IDiscordWebhookAlertManager"/>
/// <remarks>
/// Creates a new instance of the <see cref="DiscordWebhookAlertManager"/> class.
/// </remarks>
/// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> to use.</param>
/// <param name="globalOptions">The <see cref="GlobalOptions"/> to use.</param>
/// <param name="discordRolesOptions">The <see cref="DiscordRolesOptions"/> to use.</param>
/// <exception cref="ArgumentNullException">
/// - <paramref name="httpClientFactory"/> cannot be null.
/// - <paramref name="globalOptions"/> cannot be null.
/// - <paramref name="discordRolesOptions"/> cannot be null.
/// </exception>
/// <seealso cref="DiscordWebhookAlertManager"/>
public class DiscordWebhookAlertManager(
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<GlobalOptions> globalOptions,
    IOptionsMonitor<DiscordRolesOptions> discordRolesOptions
) : IDiscordWebhookAlertManager
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private readonly IOptionsMonitor<GlobalOptions> _globalOptions = globalOptions ?? throw new ArgumentNullException(nameof(globalOptions));
    private readonly IOptionsMonitor<DiscordRolesOptions> _discordRolesOptions = discordRolesOptions ?? throw new ArgumentNullException(nameof(discordRolesOptions));

    private DiscordRolesOptions _discordRolesSettings => _discordRolesOptions.CurrentValue;

    private static readonly Counter _discordWebhookAlertCounter = Metrics.CreateCounter(
        "discord_webhook_alert_total",
        "Total number of alerts sent to Discord"
    );
    private static readonly Counter _discordWebhookAlertsWithAttachmentsCounter = Metrics.CreateCounter(
        "discord_webhook_alerts_with_attachments_total",
        "Total number of alerts sent to Discord with attachments"
    );

    /// <inheritdoc cref="IDiscordWebhookAlertManager.SendAlertAsync(string, string, Color?, IEnumerable{FileAttachment})"/>
    public async Task SendAlertAsync(string topic, string message, Color? color, IEnumerable<FileAttachment> attachments = null)
    {
        if (string.IsNullOrWhiteSpace(topic)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(topic));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(message));

        if (!_globalOptions.CurrentValue.DiscordWebhookAlertingEnabled) return;

        _discordWebhookAlertCounter.Inc();

        color ??= Color.Red;

        // username based off machine info
        var username = Environment.MachineName;

        var content = string.Empty;
        if (_discordRolesSettings.AlertRoleId != default(ulong))
            content = $"<@&{_discordRolesSettings.AlertRoleId}>";

        using var client = _httpClientFactory.CreateClient();
        var url = _globalOptions.CurrentValue.DiscordWebhookUrl;
        var payload = new
        {
            username,
            content,
            embeds = new[]
            {
                new
                {
                    title = topic,
                    description = message,
                    color = color?.RawValue,
                    timestamp = DateTime.UtcNow.ToString("o")
                }
            }
        };

        var multipartContent = new MultipartFormDataContent();

        var json = JsonConvert.SerializeObject(payload);

        multipartContent.Add(new StringContent(json, Encoding.UTF8, "application/json"), "payload_json");

        if (attachments?.Any() ?? false)
        {
            _discordWebhookAlertsWithAttachmentsCounter.Inc();

            foreach (var attachment in attachments)
                multipartContent.Add(new StreamContent(attachment.Stream), attachment.FileName, attachment.FileName);
        }

        await client.PostAsync(url, multipartContent);
    }
}
