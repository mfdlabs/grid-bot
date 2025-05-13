namespace Grid.Bot.Commands.Private;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;

using Discord.Commands;

using Newtonsoft.Json;

using Utility;
using Extensions;

/// <summary>
/// Represents the command for ClientSettings.
/// </summary>
/// <remarks>
/// Construct a new instance of <see cref="ClientSettingsModule"/>.
/// </remarks>
/// <param name="clientSettingsFactory">The <see cref="IClientSettingsFactory"/>.</param>
/// <param name="clientSettingsSettings">The <see cref="ClientSettingsSettings"/>.</param>
/// <exception cref="ArgumentNullException">
/// - <paramref name="clientSettingsFactory"/> cannot be null.
/// - <paramref name="clientSettingsSettings"/> cannot be null.
/// </exception>
[LockDownCommand(BotRole.Administrator)]
[RequireBotRole(BotRole.Administrator)]
[Group("clientsettings"), Summary("Commands used for managing client settings."), Alias("cs", "client_settings")]
public class ClientSettingsModule(IClientSettingsFactory clientSettingsFactory, ClientSettingsSettings clientSettingsSettings) : ModuleBase
{
    private readonly IClientSettingsFactory _clientSettingsFactory = clientSettingsFactory ?? throw new ArgumentNullException(nameof(clientSettingsFactory));
    private readonly ClientSettingsSettings _clientSettingsSettings = clientSettingsSettings ?? throw new ArgumentNullException(nameof(clientSettingsSettings));

    /// <summary>
    /// Gets the client settings for the specified application.
    /// </summary>
    /// <param name="applicationName">The name of the application.</param>
    [Command("get_all"), Summary("Gets all client settings for the specified application.")]
    [Alias("getall", "all")]
    public async Task GetAllAsync(string applicationName)
    {
        using var _ = Context.Channel.EnterTypingState();

        var clientSettings = _clientSettingsFactory.GetSettingsForApplication(applicationName);
        if (clientSettings is null)
        {
            await this.ReplyWithReferenceAsync(
                text: "The specified application does not exist."
            );

            return;
        }

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(clientSettings, Formatting.Indented)));

        await this.ReplyWithFileAsync(stream, $"{applicationName}.json", "Here are the client settings for the specified application.");
        
    }

    /// <summary>
    /// Imports the client settings for the specified application.
    /// </summary>
    /// <param name="applicationName">The name of the application.</param>
    /// <param name="dependencies">The dependencies for the application.</param>
    /// <param name="isAllowedFromApi">Is the application allowed to be written to from the API?</param>
    [Command("import"), Summary("Imports the client settings for the specified application.")]
    public async Task ImportAsync(string applicationName, string dependencies = null, bool isAllowedFromApi = false)
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
        var contentsParsed = JsonConvert.DeserializeObject<Dictionary<string, object>>(contents);
        if (contentsParsed is null)
        {
            await this.ReplyWithReferenceAsync(
                text: "Failed to parse the client settings file."
            );

            return;
        }


        _clientSettingsFactory.WriteSettingsForApplication(applicationName, contentsParsed);

        var parsedDependencies = string.Join(",", dependencies?.Split(',', StringSplitOptions.RemoveEmptyEntries));

        if (!string.IsNullOrWhiteSpace(parsedDependencies))
        {
            _clientSettingsSettings.ClientSettingsApplicationDependencies[applicationName] = parsedDependencies;
            _clientSettingsSettings.ApplyCurrent();
        }

        if (isAllowedFromApi)
        {
            var currentPermissibleReadApplications = _clientSettingsSettings.PermissibleReadApplications.ToList();

            if (!currentPermissibleReadApplications.Contains(applicationName))
            {
                currentPermissibleReadApplications.Add(applicationName);
                _clientSettingsSettings.PermissibleReadApplications = [.. currentPermissibleReadApplications];
            }
        }

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
        using var _ = Context.Channel.EnterTypingState();

        try
        {
            if (ClientSettingsNameHelper.IsFilteredSetting(settingName))
            {
                var (filterName, filterType) = ClientSettingsNameHelper.ExtractFilteredSettingName(settingName);
                var filteredSetting = _clientSettingsFactory.GetFilteredSettingForApplication<string>(applicationName, filterName, filterType);

                await this.ReplyWithReferenceAsync(
                    embed: new EmbedBuilder()
                        .WithTitle($"{filterName} ({filterType} Filter)")
                        .AddField("Value", $"```\n{filteredSetting.Value}\n```")
                        .AddField("Filtered Ids", string.Join(", ", filteredSetting.FilteredIds))
                        .WithColor(0x00, 0xff, 0x00)
                        .Build()
                );

                return;
            }

            var applicationSetting = _clientSettingsFactory.GetSettingForApplication<string>(applicationName, settingName);

            await this.ReplyWithReferenceAsync(
                embed: new EmbedBuilder()
                    .WithTitle(settingName)
                    .WithDescription($"```\n{applicationSetting}\n```")
                    .WithColor(0x00, 0xff, 0x00)
                    .Build()
            );
        }
        catch (Exception ex)
        {
            if (ex is InvalidOperationException)
                await this.ReplyWithReferenceAsync(
                    text: "The specified application does not exist."
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
    public async Task SetAsync(string applicationName, string settingName, SettingType settingType = SettingType.String, string settingValue = "")
    {
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
                SettingType.String => settingValue,
                SettingType.Int => long.Parse(settingValue),
                SettingType.Bool => bool.Parse(settingValue),
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

        _clientSettingsFactory.SetSettingForApplication(applicationName, settingName, value, settingType);

        await this.ReplyWithReferenceAsync(text: $"Successfully set the setting `{settingName}` to `{value}` for the application `{applicationName}`.");
    }

    /// <summary>
    /// Refreshes all applications.
    /// </summary>
    [Command("refresh_all"), Summary("Refreshes all applications."), Alias("refresh")]
    public async Task RefreshAllAsync()
    {
        using var _ = Context.Channel.EnterTypingState();

        _clientSettingsFactory.Refresh();

        await this.ReplyWithReferenceAsync(
            text: "Successfully refreshed all applications."
        );
    }
}
