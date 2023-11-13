namespace Grid.Bot.Utility;

using System;
using System.Text;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;

using Newtonsoft.Json;

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
/// <param name="globalSettings">The <see cref="GlobalSettings"/> to use.</param>
/// <param name="discordRolesSettings">The <see cref="DiscordRolesSettings"/> to use.</param>
/// <exception cref="ArgumentNullException">
/// - <paramref name="localIpAddressProvider"/> cannot be null.
/// - <paramref name="httpClientFactory"/> cannot be null.
/// - <paramref name="globalSettings"/> cannot be null.
/// - <paramref name="discordRolesSettings"/> cannot be null.
/// </exception>
/// <seealso cref="DiscordWebhookAlertManager"/>
public class DiscordWebhookAlertManager(
    ILocalIpAddressProvider localIpAddressProvider,
    IHttpClientFactory httpClientFactory,
    GlobalSettings globalSettings,
    DiscordRolesSettings discordRolesSettings
) : IDiscordWebhookAlertManager
{
    private readonly ILocalIpAddressProvider _localIpAddressProvider = localIpAddressProvider ?? throw new ArgumentNullException(nameof(localIpAddressProvider));
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private readonly GlobalSettings _globalSettings = globalSettings ?? throw new ArgumentNullException(nameof(globalSettings));
    private readonly DiscordRolesSettings _discordRolesSettings = discordRolesSettings ?? throw new ArgumentNullException(nameof(discordRolesSettings));

    /// <inheritdoc cref="IDiscordWebhookAlertManager.SendAlertAsync(string, string, Color?, IEnumerable{FileAttachment})"/>
    public async Task SendAlertAsync(string topic, string message, Color? color, IEnumerable<FileAttachment> attachments = null)
    {
        if (string.IsNullOrWhiteSpace(topic)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(topic));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(message));

        if (!_globalSettings.DiscordWebhookAlertingEnabled) return;

        color ??= Color.Red;

        // username based off machine info
        var username = $"{Environment.MachineName} ({_localIpAddressProvider.AddressV4} / {_localIpAddressProvider.AddressV6})";

        var content = string.Empty;
        if (_discordRolesSettings.AlertRoleId != default(ulong))
            content = $"<@&{_discordRolesSettings.AlertRoleId}>";

        using var client = _httpClientFactory.CreateClient();
        var url = _globalSettings.DiscordWebhookUrl;
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

        await client.PostAsync(url, multipartContent);
    }
}
