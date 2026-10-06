namespace Grid.Bot;

using System;
using System.Text;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;

using Microsoft.Extensions.Options;

using Newtonsoft.Json;

using Logging;

/// <summary>
/// Handles sending alerts to a Discord webhook.
/// </summary>
/// <seealso cref="IDiscordWebhookAlertManager"/>
/// <remarks>
/// Creates a new instance of the <see cref="DiscordWebhookAlertManager"/> class.
/// </remarks>
/// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> to use.</param>
/// <param name="options">The <see cref="RecoveryOptions"/> to use.</param>
/// <param name="logger">The <see cref="ILogger"/> to use.</param>
/// /// <exception cref="ArgumentNullException">
/// - <paramref name="httpClientFactory"/> cannot be null.
/// - <paramref name="options"/> cannot be null.
/// - <paramref name="logger"/> cannot be null.
/// </exception>
/// <seealso cref="DiscordWebhookAlertManager"/>
public class DiscordWebhookAlertManager(
    IHttpClientFactory httpClientFactory,
    IOptions<RecoveryOptions> options,
    ILogger logger
) : IDiscordWebhookAlertManager
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private readonly RecoveryOptions _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc cref="IDiscordWebhookAlertManager.SendAlertAsync(string, string, Color?, IEnumerable{FileAttachment})"/>
    public async Task SendAlertAsync(string topic, string message, Color? color, IEnumerable<FileAttachment> attachments = null)
    {
        if (string.IsNullOrWhiteSpace(topic)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(topic));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(message));

        color ??= Color.Red;

        // username based off machine info
        var username = $"Grid Bot Recovery {Environment.MachineName}";

        var content = string.Empty;
        if (_settings.AlertRoleId != default(ulong))
            content = $"<@&{_settings.AlertRoleId}>";

        using var client = _httpClientFactory.CreateClient();
        var url = _settings.DiscordWebhookUrl;
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
            foreach (var attachment in attachments)
                multipartContent.Add(new StreamContent(attachment.Stream), attachment.FileName, attachment.FileName);

        try
        {
            await client.PostAsync(url, multipartContent);
        }
        catch (Exception ex)
        {
            _logger.Error("Error sending alert: {0}", ex.Message);
        }
    }
}
