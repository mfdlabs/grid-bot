namespace Grid.Bot.Utility;

using System;
using System.Text;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Collections.Concurrent;

using Discord;
using Discord.WebSocket;

using Newtonsoft.Json;

using Random;
using Networking;

using Extensions;
using Discord.Commands;


/// <summary>
/// Handles sending alerts to a Discord webhook.
/// </summary>
/// <seealso cref="IScriptLogger"/>
public class ScriptLogger : IScriptLogger
{
    private readonly ILocalIpAddressProvider _localIpAddressProvider;
    private readonly IPercentageInvoker _percentageInvoker;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ScriptsSettings _scriptsSettings;

    private readonly ConcurrentBag<string> _scriptHashes = new();

    /// <summary>
    /// Creates a new instance of the <see cref="ScriptLogger"/> class.
    /// </summary>
    /// <param name="localIpAddressProvider">The <see cref="ILocalIpAddressProvider"/> to use.</param>
    /// <param name="percentageInvoker">The <see cref="IPercentageInvoker"/> to use.</param>
    /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> to use.</param>
    /// <param name="scriptsSettings">The <see cref="ScriptsSettings"/> to use.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="localIpAddressProvider"/> cannot be null.
    /// - <paramref name="percentageInvoker"/> cannot be null.
    /// - <paramref name="httpClientFactory"/> cannot be null.
    /// - <paramref name="scriptsSettings"/> cannot be null.
    /// </exception>
    public ScriptLogger(
        ILocalIpAddressProvider localIpAddressProvider,
        IPercentageInvoker percentageInvoker,
        IHttpClientFactory httpClientFactory,
        ScriptsSettings scriptsSettings
    )
    {
        _localIpAddressProvider = localIpAddressProvider ?? throw new ArgumentNullException(nameof(localIpAddressProvider));
        _percentageInvoker = percentageInvoker ?? throw new ArgumentNullException(nameof(percentageInvoker));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _scriptsSettings = scriptsSettings ?? throw new ArgumentNullException(nameof(scriptsSettings));

        foreach (var hash in _scriptsSettings.LoggedScriptHashes)
            _scriptHashes.Add(hash);

        Task.Factory.StartNew(PersistLoggedScriptHashes, TaskCreationOptions.LongRunning);
    }

    private void PersistLoggedScriptHashes()
    {
        while (true)
        {
            Task.Delay(_scriptsSettings.LoggedScriptHashesPersistInterval).Wait();

            if (_scriptsSettings.LoggedScriptHashes.SequenceEqual(_scriptHashes)) continue;

            _scriptsSettings.LoggedScriptHashes = [.. _scriptHashes];
        }
    }

    /// <inheritdoc cref="IScriptLogger.LogScriptAsync(string, IInteractionContext)"/>
    public async Task LogScriptAsync(string script, IInteractionContext context)
    {
        if (string.IsNullOrWhiteSpace(script)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(script));
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        if (!_percentageInvoker.CanInvoke(_scriptsSettings.ScriptLoggingPercentage)) return;
        if (context.Interaction is not SocketInteraction interaction) return;

        // username based off machine info
        var userInfo = context.User.ToString();
        var guildInfo = interaction.GetGuild(context.Client)?.ToString() ?? "DMs";
        var channelInfo = interaction.GetChannelAsString();

        await DoLogScriptAsync(script, userInfo, guildInfo, channelInfo);
    }

    /// <inheritdoc cref="IScriptLogger.LogScriptAsync(string, ICommandContext)"/>
    public async Task LogScriptAsync(string script, ICommandContext context)
    {
        if (string.IsNullOrWhiteSpace(script)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(script));
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        if (!_percentageInvoker.CanInvoke(_scriptsSettings.ScriptLoggingPercentage)) return;
        if (context.Message is not SocketUserMessage message) return;

        // username based off machine info
        var userInfo = context.User.ToString();
        var guildInfo = context.Guild?.Id.ToString() ?? "DMs";
        var channelInfo = message.Channel?.ToString();
        
        await DoLogScriptAsync(script, userInfo, guildInfo, channelInfo);
    }

    private async Task DoLogScriptAsync(string script, string userInfo, string guildInfo, string channelInfo)
    {
        var username = $"{Environment.MachineName} ({_localIpAddressProvider.AddressV4} / {_localIpAddressProvider.AddressV6})";

        // Get a SHA256 hash of the script (hex)
        var scriptHash = string.Join("", SHA256.HashData(Encoding.UTF8.GetBytes(script)).Select(b => b.ToString("x2")));
        var content = $"""
                **User:** {userInfo}
                **Guild:** {guildInfo}
                **Channel:** {channelInfo}
                **Script Hash:** {scriptHash}
                """;

        using var client = _httpClientFactory.CreateClient();

        var url = _scriptsSettings.ScriptLoggingDiscordWebhookUrl;


        if (_scriptHashes.Contains(scriptHash))
        {
            // Just log the hash
            content += "\n\n**Script already logged**";

            var existsPayload = new
            {
                username,
                content
            };

            var existsJson = JsonConvert.SerializeObject(existsPayload);

            await client.PostAsync(url, new StringContent(existsJson, Encoding.UTF8, "application/json"));

            return;
        }

        var payload = new
        {
            username,
            content = string.Empty,
            embeds = new[]
            {
                new
                {
                    title = "User information",
                    description = content,
                    color = Color.Green.RawValue,
                    timestamp = DateTime.UtcNow.ToString("o")
                }
            }
        };

        var multipartContent = new MultipartFormDataContent();

        var json = JsonConvert.SerializeObject(payload);

        multipartContent.Add(new StringContent(json, Encoding.UTF8, "application/json"), "payload_json");
        multipartContent.Add(new StringContent(script, Encoding.UTF8, "text/plain"), $"{scriptHash}.lua", $"{scriptHash}.lua");

        await client.PostAsync(url, multipartContent);

        // Add the hash to the list of logged hashes
        _scriptHashes.Add(scriptHash);
    }

}
