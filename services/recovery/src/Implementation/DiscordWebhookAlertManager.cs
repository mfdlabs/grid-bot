namespace Grid.Bot;

using System;
using System.Text;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;

using Newtonsoft.Json;

using Logging;
using Networking;

/// <summary>
/// Handles sending alerts to a Discord webhook.
/// </summary>
/// <seealso cref="IDiscordWebhookAlertManager"/>
/// <remarks>
/// Creates a new instance of the <see cref="DiscordWebhookAlertManager"/> class.
/// </remarks>
/// <param name="localIpAddressProvider">The <see cref="ILocalIpAddressProvider"/> to use.</param>
/// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> to use.</param>
/// <param name="settings">The <see cref="ISettings"/> to use.</param>
/// <param name="logger">The <see cref="ILogger"/> to use.</param>
/// /// <exception cref="ArgumentNullException">
/// - <paramref name="localIpAddressProvider"/> cannot be null.
/// - <paramref name="httpClientFactory"/> cannot be null.
/// - <paramref name="settings"/> cannot be null.
/// - <paramref name="logger"/> cannot be null.
/// </exception>
/// <seealso cref="DiscordWebhookAlertManager"/>
public class DiscordWebhookAlertManager(
    ILocalIpAddressProvider localIpAddressProvider,
    IHttpClientFactory httpClientFactory,
    ISettings settings,
    ILogger logger
) : IDiscordWebhookAlertManager
{
    private readonly ILocalIpAddressProvider _localIpAddressProvider = localIpAddressProvider ?? throw new ArgumentNullException(nameof(localIpAddressProvider));
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private readonly ISettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc cref="IDiscordWebhookAlertManager.SendAlertAsync(string, string, Color?, IEnumerable{FileAttachment})"/>
    public async Task SendAlertAsync(string topic, string message, Color? color, IEnumerable<FileAttachment> attachments = null)
    {
        if (string.IsNullOrWhiteSpace(topic)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(topic));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(message));

        color ??= Color.Red;

        // username based off machine info
        var username = $"Grid Bot Recovery {Environment.MachineName} ({_localIpAddressProvider.AddressV4} / {_localIpAddressProvider.AddressV6})";

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
