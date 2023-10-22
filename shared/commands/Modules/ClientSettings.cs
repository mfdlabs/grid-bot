namespace Grid.Bot.Interactions;

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.Interactions;

using Newtonsoft.Json;

using ClientSettings.Client;

using Extensions;

/// <summary>
/// Represents the interaction for ClientSettings.
/// </summary>
[Group("clientsettings", "Manage the client settings.")]
[RequireBotRole(BotRole.Administrator)]
public class ClientSettingsModule : InteractionModuleBase<ShardedInteractionContext>
{
    private readonly IClientSettingsClient _clientSettingsClient;
    private readonly ClientSettingsClientSettings _clientSettingsClientSettings;

    /// <summary>
    /// Represents the type of client setting.
    /// </summary>
    public enum ClientSettingType
    {
        /// <summary>
        /// Represents a string client setting.
        /// </summary>
        [ChoiceDisplay("string")]
        String,

        /// <summary>
        /// Represents an integer client setting.
        /// </summary>
        [ChoiceDisplay("int")]
        Int,

        /// <summary>
        /// Represents a boolean client setting.
        /// </summary>
        [ChoiceDisplay("bool")]
        Bool,
    }

    /// <summary>
    /// Construct a new instance of <see cref="ClientSettingsModule"/>.
    /// </summary>
    /// <param name="clientSettingsClient">The <see cref="IClientSettingsClient"/>.</param>
    /// <param name="clientSettingsClientSettings">The <see cref="ClientSettingsClientSettings"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="clientSettingsClient"/> cannot be null.
    /// - <paramref name="clientSettingsClientSettings"/> cannot be null.
    /// </exception>
    public ClientSettingsModule(IClientSettingsClient clientSettingsClient, ClientSettingsClientSettings clientSettingsClientSettings)
    {
        _clientSettingsClient = clientSettingsClient ?? throw new ArgumentNullException(nameof(clientSettingsClient));
        _clientSettingsClientSettings = clientSettingsClientSettings ?? throw new ArgumentNullException(nameof(clientSettingsClientSettings));
    }

    /// <summary>
    /// Gets the client settings for the specified application.
    /// </summary>
    /// <param name="applicationName">The name of the application.</param>
    [SlashCommand("get_all", "Gets all client settings for the specified application.")]
    public async Task GetAllAsync(
        [Summary("application_name", "The name of the application to get the client settings for.")]
        string applicationName
    )
    {
        if (string.IsNullOrWhiteSpace(applicationName))
        {
            await FollowupAsync(
                text: "Please specify an application name."
            );

            return;
        }

        try
        {
            var clientSettings = await _clientSettingsClient.GetApplicationSettingsAsync(applicationName).ConfigureAwait(false);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(clientSettings, Formatting.Indented)));

            await FollowupWithFileAsync(stream, $"{applicationName}.json", "Here are the client settings for the specified application.");
        }
        catch (ApiException ex)
        {
            if (ex.StatusCode == (int)HttpStatusCode.BadRequest)
                await FollowupAsync(
                    text: "The specified application does not exist."
                );
            else if (ex.StatusCode == 403)
                await FollowupAsync(
                    text: "The specified application does not have permission to be written to client settings API."
                );
            else
                throw;
        }
    }

    /// <summary>
    /// Imports the client settings for the specified application.
    /// </summary>
    /// <param name="applicationName">The name of the application.</param>
    /// <param name="applicationSettings">The client settings for the application.</param>
    /// <param name="dependencies">The dependencies for the application.</param>
    /// <param name="reference">The reference for the application.</param>
    /// <param name="isAllowedFromApi">Is the application allowed to be written to from the API?</param>
    [SlashCommand("import", "Imports the client settings for the specified application.")]
    public async Task ImportAsync(
        [Summary("application_name", "The name of the application to import the client settings for.")]
        string applicationName,

        [Summary("application_settings", "The client settings for the application.")]
        IAttachment applicationSettings,

        [Summary("dependencies", "The dependencies for the application.")]
        string dependencies = null,

        [Summary("reference", "The reference for the application.")]
        string reference = null,

        [Summary("is_allowed_from_api", "Is the application allowed to be written to from the API?")]
        bool isAllowedFromApi = false
    )
    {
        if (string.IsNullOrWhiteSpace(applicationName))
        {
            await FollowupAsync(
                text: "Please specify an application name."
            );

            return;
        }

        if (applicationSettings is null)
        {
            await FollowupAsync(
                text: "Please specify an application settings file."
            );

            return;
        }

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

        await FollowupAsync(
            text: "Successfully imported the client settings for the specified application."
        );
    }

    /// <summary>
    /// Gets a client setting for the specified application.
    /// </summary>
    /// <param name="applicationName">The name of the application.</param>
    /// <param name="settingName">The name of the setting.</param>
    [SlashCommand("get", "Gets a client setting for the specified application.")]
    public async Task GetAsync(
        [Summary("application_name", "The name of the application to get the client setting for.")]
        string applicationName,

        [Summary("setting_name", "The name of the setting to get.")]
        string settingName
    )
    {
        if (string.IsNullOrWhiteSpace(applicationName))
        {
            await FollowupAsync(
                text: "Please specify an application name."
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(settingName))
        {
            await FollowupAsync(
                text: "Please specify a setting name."
            );

            return;
        }

        try
        {
            var applicationSetting = await _clientSettingsClient.GetClientApplicationSettingAsync(applicationName, settingName).ConfigureAwait(false);

            await FollowupAsync(
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
                await FollowupAsync(
                    text: "The specified application does not exist."
                );
            else if (ex.StatusCode == 403)
                await FollowupAsync(
                    text: "The specified application does not have permission to be written to client settings API."
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
    [SlashCommand("set", "Sets a client setting for the specified application.")]
    public async Task SetAsync(
        [Summary("application_name", "The name of the application to set the client setting for.")]
        string applicationName,

        [Summary("setting_name", "The name of the setting to set.")]
        string settingName,

        [Summary("setting_type", "The type of the setting to set.")]
        ClientSettingType settingType = ClientSettingType.String,

        [Summary("setting_value", "The value of the setting to set.")]
        string settingValue = ""
    )
    {
        if (string.IsNullOrWhiteSpace(applicationName))
        {
            await FollowupAsync(
                text: "Please specify an application name."
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(settingName))
        {
            await FollowupAsync(
                text: "Please specify a setting name."
            );

            return;
        }

        if (settingValue is null)
        {
            await FollowupAsync(
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
            await FollowupAsync(
                text: $"Failed to parse setting value: {ex.Message}"
            );

            return;
        }

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

            await FollowupAsync(
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
                await FollowupAsync(
                    text: "The specified application does not exist."
                );
            else if (ex.StatusCode == 403)
                await FollowupAsync(
                    text: "The specified application does not have permission to be written to client settings API."
                );
            else
                throw;
        }
    }

    /// <summary>
    /// Refreshes all applications.
    /// </summary>
    [SlashCommand("refresh_all", "Refreshes all applications.")]
    public async Task RefreshAllAsync()
    {
        await _clientSettingsClient.RefreshAllClientApplicationSettingsAsync(_clientSettingsClientSettings.ClientSettingsApiKey).ConfigureAwait(false);

        await FollowupAsync(
            text: "Successfully refreshed all applications."
        );
    }
}
