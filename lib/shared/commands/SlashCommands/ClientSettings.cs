#if WE_LOVE_EM_SLASH_COMMANDS

using ClientSettings.Client;

namespace Grid.Bot.SlashCommands;

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.WebSocket;

using Newtonsoft.Json;

using Text.Extensions;

using Utility;
using Extensions;
using Interfaces;

/// <summary>
/// Command for interacting with the Client Settings API.
/// </summary>
internal class ClientSettings : ISlashCommandHandler
{
    

    /// <inheritdoc cref="ISlashCommandHandler.Description"/>
    public string Description => "Manage client settings globally";

    /// <inheritdoc cref="ISlashCommandHandler.Name"/>
    public string Name => "clientsettings";

    /// <inheritdoc cref="ISlashCommandHandler.IsInternal"/>
    public bool IsInternal => true;

    /// <inheritdoc cref="ISlashCommandHandler.IsEnabled"/>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc cref="ISlashCommandHandler.Options"/>
    public SlashCommandOptionBuilder[] Options => new[]
    {
        new SlashCommandOptionBuilder()
            .WithName("get_all")
            .WithDescription("Gets all application settings for the given group.")
            .WithType(ApplicationCommandOptionType.SubCommand)
            .AddOption("application_name", ApplicationCommandOptionType.String, "The name of the group", true),

        new SlashCommandOptionBuilder()
            .WithName("import")
            .WithDescription("Imports application settings for the given group.")
            .WithType(ApplicationCommandOptionType.SubCommand)
            .AddOption("application_name", ApplicationCommandOptionType.String, "The name of the group", true)
            .AddOption("application_settings", ApplicationCommandOptionType.Attachment, "The application settings as a JSON file.", true)
            .AddOption("dependencies", ApplicationCommandOptionType.String, "The dependencies for the application.", false)
            .AddOption("reference", ApplicationCommandOptionType.String, "The group this application is referencing", false)
            .AddOption("is_allowed_from_api", ApplicationCommandOptionType.Boolean, "Is this application allowed to be queried from client settings service?", false),

        new SlashCommandOptionBuilder()
            .WithName("get")
            .WithDescription("Gets an application setting.")
            .WithType(ApplicationCommandOptionType.SubCommand)
            .AddOption("application_name", ApplicationCommandOptionType.String, "The name of the group", true)
            .AddOption("setting_name", ApplicationCommandOptionType.String, "The name of the setting", true),

        new SlashCommandOptionBuilder()
            .WithName("set")
            .WithDescription("Sets an application's setting.")
            .WithType(ApplicationCommandOptionType.SubCommand)
            .AddOption("application_name", ApplicationCommandOptionType.String, "The name of the group", true)
            .AddOption("setting_name", ApplicationCommandOptionType.String, "The name of the setting", true)
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("type")
                .WithDescription("The type of the setting")
                .WithRequired(true)
                .AddChoice("string", "string")
                .AddChoice("bool", "bool")
                .AddChoice("int", "int")
                .WithType(ApplicationCommandOptionType.String)
            )
            .AddOption("value", ApplicationCommandOptionType.String, "The name of the setting", true),

        new SlashCommandOptionBuilder()
            .WithName("refresh")
            .WithDescription("Refreshes all the client settings")
            .WithType(ApplicationCommandOptionType.SubCommand)
    };

    private static readonly IClientSettingsClient _clientSettingsClient = new ClientSettingsClient(
        ClientSettingsClientSettings.Singleton.ClientSettingsApiBaseUrl,
        ClientSettingsClientSettings.Singleton.ClientSettingsCertificateValidationEnabled
    );

    /// <inheritdoc cref="ISlashCommandHandler.ExecuteAsync(SocketSlashCommand)"/>
    public async Task ExecuteAsync(SocketSlashCommand command)
    {
        if (!await command.RejectIfNotAdminAsync()) return;

        string applicationName;
        string settingName;

        var subCommand = command.Data.GetSubCommand();
        var option = subCommand.Name;

        switch (option)
        {
            case "get_all":
                applicationName = subCommand.GetOptionValue<string>("application_name");

                if (applicationName.IsNullOrEmpty())
                {
                    await command.RespondEphemeralAsync("The parameter 'application_name' is required!");

                    return;
                }

                try
                {
                    var applicationSettings = await _clientSettingsClient.GetApplicationSettingsAsync(applicationName);

                    await command.RespondWithFilePublicAsync(
                        new MemoryStream(Encoding.UTF8.GetBytes(applicationSettings.ToJsonPretty())),
                        $"{applicationName}.json"
                    );
                }
                catch (ApiException)
                {
                    await command.RespondEphemeralAsync($"Unknown application [{applicationName}]!");
                }

                return;
            case "import":
                applicationName = subCommand.GetOptionValue<string>("application_name");

                if (applicationName.IsNullOrEmpty())
                {
                    await command.RespondEphemeralAsync("The parameter 'application_name' is required!");

                    return;
                }

                var attachment = subCommand.GetOptionValue<IAttachment>("application_settings");

                if (attachment == null)
                {
                    await command.RespondEphemeralAsync("The parameter 'application_settings' is required!");

                    return;
                }

                var contents = attachment.GetAttachmentContentsAscii().EscapeQuotes();

                var request = new ImportClientApplicationSettingsRequest
                {
                    ApplicationName = applicationName,
                    ApplicationSettings = JsonConvert.DeserializeObject<Dictionary<string, object>>(contents)
                };

                var dependencies = subCommand.GetOptionValue<string>("dependencies")?.Split(',');
                if (dependencies?.Length > 0)
                    request.Dependencies = dependencies;

                var reference = subCommand.GetOptionValue<string>("reference");
                if (!reference.IsNullOrEmpty())
                    request.Reference = reference;

                request.IsAllowedFromClientSettingsService = subCommand.GetOptionValue("is_allowed_from_api", true);

                await _clientSettingsClient.ImportApplicationSettingAsync(
                    ClientSettingsClientSettings.Singleton.ClientSettingsApiKey,
                    request
                );

                await command.RespondPublicAsync(
                    $"Successfully imported application [{applicationName}]!"
                );

                return;
            case "get":
                applicationName = subCommand.GetOptionValue<string>("application_name");

                if (applicationName.IsNullOrEmpty())
                {
                    await command.RespondEphemeralAsync("The parameter 'application_name' is required!");

                    return;
                }

                settingName = subCommand.GetOptionValue<string>("setting_name");

                if (settingName.IsNullOrEmpty())
                {
                    await command.RespondEphemeralAsync("The parameter 'setting_name' is required!");

                    return;
                }

                try
                {
                    var applicationSetting = await _clientSettingsClient.GetClientApplicationSettingAsync(applicationName, settingName);

                    await command.RespondPublicAsync(
                        embed: new EmbedBuilder()
                            .WithTitle(settingName)
                            .WithDescription($"```\n{applicationSetting.Value}\n```")
                            .WithColor(0x00, 0xff, 0x00)
                            .Build()
                    );
                }
                catch (ApiException)
                {
                    await command.RespondEphemeralAsync($"Unknown application [{applicationName}] setting [{settingName}]!");
                }

                return;
            case "set":
                applicationName = subCommand.GetOptionValue<string>("application_name");

                if (applicationName.IsNullOrEmpty())
                {
                    await command.RespondEphemeralAsync("The parameter 'application_name' is required!");

                    return;
                }

                settingName = subCommand.GetOptionValue<string>("setting_name");

                if (settingName.IsNullOrEmpty())
                {
                    await command.RespondEphemeralAsync("The parameter 'setting_name' is required!");

                    return;
                }

                var type = subCommand.GetOptionValue<string>("type")?.ToLower();

                if (type.IsNullOrEmpty())
                {
                    await command.RespondEphemeralAsync("The parameter 'type' is required!");

                    return;
                }

                var strValue = subCommand.GetOptionValue<string>("value");
                if (strValue == null)
                {
                    await command.RespondEphemeralAsync("The parameter 'value' is required!");

                    return;
                }

                object value;

                try
                {
                    value = type switch
                    {
                        "bool" => Convert.ToBoolean(strValue),
                        "int" => Convert.ToDouble(strValue),
                        _ => strValue,
                    };
                }
                catch (FormatException ex)
                {
                    await command.RespondEphemeralAsync($"The value [{strValue}] could not be parsed to [{type}] because: {ex.Message}");

                    return;
                }

                try
                {
                    var diff = await _clientSettingsClient.SetClientApplicationSettingAsync(
                        ClientSettingsClientSettings.Singleton.ClientSettingsApiKey,
                        new SetClientApplicationSettingRequest
                        {
                            ApplicationName = applicationName,
                            SettingName = settingName,
                            Value = value,
                        }
                    );

                    await command.RespondPublicAsync(
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
                    await command.RespondEphemeralAsync($"Unknown application [{applicationName}] setting [{settingName}]. Exception: {ex.Message}!");
                }

                return;
            case "refresh":
                await _clientSettingsClient.RefreshAllClientApplicationSettingsAsync(
                    ClientSettingsClientSettings.Singleton.ClientSettingsApiKey
                );

                await command.RespondPublicAsync(
                    "Successfully refreshed all applications!"
                );

                return;
        }
    }
}

#endif
