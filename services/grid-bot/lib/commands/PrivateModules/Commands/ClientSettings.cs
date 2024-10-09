namespace Grid.Bot.Commands.Private;

using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;

using Discord.Commands;

using Newtonsoft.Json;

using ClientSettings.Client;

using Utility;
using Extensions;

/// <summary>
/// Represents the command for ClientSettings.
/// </summary>
/// <remarks>
/// Construct a new instance of <see cref="ClientSettingsModule"/>.
/// </remarks>
/// <param name="clientSettingsClient">The <see cref="IClientSettingsClient"/>.</param>
/// <param name="clientSettingsClientSettings">The <see cref="ClientSettingsClientSettings"/>.</param>
/// <exception cref="ArgumentNullException">
/// - <paramref name="clientSettingsClient"/> cannot be null.
/// - <paramref name="clientSettingsClientSettings"/> cannot be null.
/// </exception>
[LockDownCommand(BotRole.Administrator)]
[RequireBotRole(BotRole.Administrator)]
[Group("clientsettings"), Summary("Commands used for managing client settings."), Alias("cs", "client_settings")]
public class ClientSettingsModule(IClientSettingsClient clientSettingsClient, ClientSettingsClientSettings clientSettingsClientSettings) : ModuleBase
{
    private readonly IClientSettingsClient _clientSettingsClient = clientSettingsClient ?? throw new ArgumentNullException(nameof(clientSettingsClient));
    private readonly ClientSettingsClientSettings _clientSettingsClientSettings = clientSettingsClientSettings ?? throw new ArgumentNullException(nameof(clientSettingsClientSettings));

    /// <summary>
    /// Represents the type of client setting.
    /// </summary>
    public enum ClientSettingType
    {
        /// <summary>
        /// Represents a string client setting.
        /// </summary>
        String,

        /// <summary>
        /// Represents an integer client setting.
        /// </summary>
        Int,

        /// <summary>
        /// Represents a boolean client setting.
        /// </summary>
        Bool,
    }

    /// <summary>
    /// Gets the client settings for the specified application.
    /// </summary>
    /// <param name="applicationName">The name of the application.</param>
    /// <param name="useApiKey">Should the API key be used? This will allow the application to be returned from the client settings API even if $allowed on the backend is false.</param>
    [Command("get_all"), Summary("Gets all client settings for the specified application.")]
    [Alias("getall", "all")]
    public async Task GetAllAsync(string applicationName, bool useApiKey = false)
    {
        using var _ = Context.Channel.EnterTypingState();

        try
        {
            var clientSettings = await _clientSettingsClient.GetApplicationSettingsAsync(
                applicationName,
                useApiKey ? _clientSettingsClientSettings.ClientSettingsApiKey : null
            ).ConfigureAwait(false);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(clientSettings, Formatting.Indented)));

            await this.ReplyWithFileAsync(stream, $"{applicationName}.json", "Here are the client settings for the specified application.");
        }
        catch (ApiException ex)
        {
            if (ex.StatusCode == (int)HttpStatusCode.BadRequest)
                await this.ReplyWithReferenceAsync(
                    "The specified application does not exist."
                );
            else if (ex.StatusCode == (int)HttpStatusCode.Unauthorized)
                await this.ReplyWithReferenceAsync(
                    "The specified application cannot be returned from the client settings API without an API key. Please set the use_api_key parameter to true."
                );
            else
                throw;
        }
    }

    /// <summary>
    /// Imports the client settings for the specified application.
    /// </summary>
    /// <param name="applicationName">The name of the application.</param>
    /// <param name="dependencies">The dependencies for the application.</param>
    /// <param name="reference">The reference for the application.</param>
    /// <param name="isAllowedFromApi">Is the application allowed to be written to from the API?</param>
    [Command("import"), Summary("Imports the client settings for the specified application.")]
    public async Task ImportAsync(string applicationName, string dependencies = null, string reference = null, bool isAllowedFromApi = false)
    {
        var applicationSettings = Context.Message.Attachments.FirstOrDefault();
        if (applicationSettings is null)
        {
            await this.ReplyWithReferenceAsync(
                text: "Please attach the client settings file."
            );

            return;
        }

        using var _ = Context.Channel.EnterTypingState();

        var contents = await applicationSettings.GetAttachmentContentsAscii();

        var request = new ImportClientApplicationSettingsRequest
        {
            ApplicationName = applicationName,
            ApplicationSettings = JsonConvert.DeserializeObject<Dictionary<string, object>>(contents),
        };

        var parsedDependencies = dependencies?.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (parsedDependencies is not null && parsedDependencies.Length > 0)
            request.Dependencies = parsedDependencies;

        if (!string.IsNullOrWhiteSpace(reference))
            request.Reference = reference;

        request.IsAllowedFromClientSettingsService = isAllowedFromApi;

        await _clientSettingsClient.ImportApplicationSettingAsync(
            _clientSettingsClientSettings.ClientSettingsApiKey,
            request
        ).ConfigureAwait(false);

        await this.ReplyWithReferenceAsync(
            text: "Successfully imported the client settings for the specified application."
        );
    }

    /// <summary>
    /// Gets a client setting for the specified application.
    /// </summary>
    /// <param name="applicationName">The name of the application.</param>
    /// <param name="settingName">The name of the setting.</param>
    [Command("get"), Summary("Gets a client setting for the specified application.")]
    public async Task GetAsync(string applicationName, string settingName)
    {
        if (string.IsNullOrWhiteSpace(applicationName))
        { 
            await this.ReplyWithReferenceAsync(
                text: "Please specify an application name."
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(settingName))
        {
            await this.ReplyWithReferenceAsync(
                text: "Please specify a setting name."
            );

            return;
        }

        using var _ = Context.Channel.EnterTypingState();

        try
        {
            var applicationSetting = await _clientSettingsClient.GetClientApplicationSettingAsync(applicationName, settingName).ConfigureAwait(false);

            await this.ReplyWithReferenceAsync(
                embed: new EmbedBuilder()
                    .WithTitle(settingName)
                    .WithDescription($"```\n{applicationSetting.Value}\n```")
                    .WithColor(0x00, 0xff, 0x00)
                    .Build()
            );
        }
        catch (ApiException ex)
        {
            if (ex.StatusCode == (int)HttpStatusCode.BadRequest)
                await this.ReplyWithReferenceAsync(
                    text: "The specified application does not exist."
                );
            else if (ex.StatusCode == (int)HttpStatusCode.NotFound)
                await this.ReplyWithReferenceAsync(
                    text: "The specified application setting does not exist."
                );
            else
                throw;
        }
    }

    /// <summary>
    /// Sets a client setting for the specified application.
    /// </summary>
    /// <param name="applicationName">The name of the application.</param>
    /// <param name="settingName">The name of the setting.</param>
    /// <param name="settingType">The type of the setting.</param>
    /// <param name="settingValue">The value of the setting.</param>
    [Command("set"), Summary("Sets a client setting for the specified application.")]
    public async Task SetAsync(string applicationName, string settingName, ClientSettingType settingType = ClientSettingType.String, string settingValue = "")
    {
        if (string.IsNullOrWhiteSpace(applicationName))
        {
            await this.ReplyWithReferenceAsync(
                text: "Please specify an application name."
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(settingName))
        {
            await this.ReplyWithReferenceAsync(
                text: "Please specify a setting name."
            );

            return;
        }

        if (settingValue is null)
        {
            await this.ReplyWithReferenceAsync(
                text: "Please specify a setting value."
            );

            return;
        }

        object value;

        try
        {
            value = settingType switch
            {
                ClientSettingType.String => settingValue,
                ClientSettingType.Int => int.Parse(settingValue),
                ClientSettingType.Bool => bool.Parse(settingValue),
                _ => throw new InvalidOperationException($"Unknown setting type: {settingType}"),
            };
        }
        catch (FormatException ex)
        {
            await this.ReplyWithReferenceAsync(
                text: $"Failed to parse setting value: {ex.Message}"
            );

            return;
        }

        using var _ = Context.Channel.EnterTypingState();

        try
        {
            var diff = await _clientSettingsClient.SetClientApplicationSettingAsync(
                _clientSettingsClientSettings.ClientSettingsApiKey,
                new SetClientApplicationSettingRequest
                {
                    ApplicationName = applicationName,
                    SettingName = settingName,
                    Value = value,
                }
            ).ConfigureAwait(false);

            await this.ReplyWithReferenceAsync(
                embed: new EmbedBuilder()
                    .WithTitle(settingName)
                    .AddField("Previous Value", diff.OldValue?.Value ?? "null")
                    .AddField("New Value", value)
                    .AddField("Did Update", diff.DidUpdate)
                    .WithColor(0x00, 0xff, 0x00)
                    .Build()
            );
        }
        catch (ApiException ex)
        {
            if (ex.StatusCode == (int)HttpStatusCode.BadRequest)
                await this.ReplyWithReferenceAsync(
                    text: "The specified application does not exist."
                );
            else
                throw;
        }
    }

    /// <summary>
    /// Refreshes all applications.
    /// </summary>
    [Command("refresh_all"), Summary("Refreshes all applications."), Alias("refresh")]
    public async Task RefreshAllAsync()
    {
        using var _ = Context.Channel.EnterTypingState();

        await _clientSettingsClient.RefreshAllClientApplicationSettingsAsync(_clientSettingsClientSettings.ClientSettingsApiKey).ConfigureAwait(false);

        await this.ReplyWithReferenceAsync(
            text: "Successfully refreshed all applications."
        );
    }
}
