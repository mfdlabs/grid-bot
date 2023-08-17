#if WE_LOVE_EM_SLASH_COMMANDS

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.WebSocket;

using Newtonsoft.Json;

using Text.Extensions;
using ClientSettings.Client;
using Reflection.Extensions;

using Grid.Bot.Utility;
using Grid.Bot.Extensions;
using Grid.Bot.Interfaces;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

namespace Grid.Bot.SlashCommands
{
    internal class ClientSettings : IStateSpecificSlashCommandHandler
    {
        private static IClientSettingsClient _Client = new ClientSettingsClient(
            global::Grid.Bot.Properties.Settings.Default.ClientSettingsApiBaseUrl,
            global::Grid.Bot.Properties.Settings.Default.ClientSettingsCertificateValidationEnabled
        );

        public string CommandDescription => "Manage client settings globally";
        public string Name => "clientsettings";
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;
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

        public async Task Invoke(SocketSlashCommand command)
        {
            if (!await command.RejectIfNotAdminAsync()) return;

            string applicationName;
            string settingName;

            var subCommand = command.Data.GetSubCommand();
            var option = subCommand.Name;

            switch (option)
            {
                case "get_all":
                    applicationName = subCommand.GetOptionValue("application_name")?.ToString();

                    if (applicationName.IsNullOrEmpty())
                    {
                        await command.RespondEphemeralAsync("The parameter 'application_name' is required!");
                        return;
                    }

                    try
                    {
                        var applicationSettings = await _Client.GetApplicationSettingsAsync(applicationName);

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
                    applicationName = subCommand.GetOptionValue("application_name")?.ToString();

                    if (applicationName.IsNullOrEmpty())
                    {
                        await command.RespondEphemeralAsync("The parameter 'application_name' is required!");
                        return;
                    }

                    var attachment = (IAttachment)subCommand.GetOptionValue("application_settings");

                    if (attachment == null)
                    {
                        await command.RespondEphemeralAsync("The parameter 'application_settings' is required!");
                        return;
                    }

                    var contents = attachment.GetAttachmentContentsAscii().EscapeQuotes();

                    var request = new ImportClientApplicationSettingsRequest();

                    request.ApplicationName = applicationName;
                    request.ApplicationSettings = JsonConvert.DeserializeObject<Dictionary<string, object>>(contents);

                    var dependencies = subCommand.GetOptionValue("dependencies")?.ToString()?.Split(',');
                    if (dependencies?.Length > 0)
                    {
                        request.Dependencies = dependencies;
                    }

                    var reference = subCommand.GetOptionValue("reference")?.ToString();
                    if (!reference.IsNullOrEmpty())
                    {
                        request.Reference = reference;
                    }

                    request.IsAllowedFromClientSettingsService = subCommand.GetOptionValue("is_allowed_from_api")?.ToBoolean() ?? true;

                    await _Client.ImportApplicationSettingAsync(
                        global::Grid.Bot.Properties.Settings.Default.ClientSettingsApiKey,
                        request
                    );

                    await command.RespondPublicAsync(
                        $"Successfully imported application [{applicationName}]!"
                    );

                    return;
                case "get":
                    applicationName = subCommand.GetOptionValue("application_name")?.ToString();

                    if (applicationName.IsNullOrEmpty())
                    {
                        await command.RespondEphemeralAsync("The parameter 'application_name' is required!");
                        return;
                    }

                    settingName = subCommand.GetOptionValue("setting_name")?.ToString();

                    if (settingName.IsNullOrEmpty())
                    {
                        await command.RespondEphemeralAsync("The parameter 'setting_name' is required!");
                        return;
                    }

                    try
                    {
                        var applicationSetting = await _Client.GetClientApplicationSettingAsync(applicationName, settingName);

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
                    applicationName = subCommand.GetOptionValue("application_name")?.ToString();

                    if (applicationName.IsNullOrEmpty())
                    {
                        await command.RespondEphemeralAsync("The parameter 'application_name' is required!");
                        return;
                    }

                    settingName = subCommand.GetOptionValue("setting_name")?.ToString();

                    if (settingName.IsNullOrEmpty())
                    {
                        await command.RespondEphemeralAsync("The parameter 'setting_name' is required!");
                        return;
                    }

                    var type = subCommand.GetOptionValue("type")?.ToString()?.ToLower();

                    if (type.IsNullOrEmpty())
                    {
                        await command.RespondEphemeralAsync("The parameter 'type' is required!");
                        return;
                    }

                    var strValue = subCommand.GetOptionValue("value")?.ToString();
                    if (strValue == null)
                    {
                        await command.RespondEphemeralAsync("The parameter 'value' is required!");
                        return;
                    }

                    object value;

                    try
                    {
                        switch (type)
                        {
                            case "bool":
                                value = Convert.ToBoolean(strValue);
                                break;
                            case "int":
                                value = Convert.ToDouble(strValue);
                                break;
                            default:
                                value = strValue;
                                break;
                        }
                    }
                    catch (FormatException ex)
                    {
                        await command.RespondEphemeralAsync($"The value [{strValue}] could not be parsed to [{type}] because: {ex.Message}");

                        return;
                    }

                    try
                    {
                        var diff = await _Client.SetClientApplicationSettingAsync(
                            global::Grid.Bot.Properties.Settings.Default.ClientSettingsApiKey,
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
                    await _Client.RefreshAllClientApplicationSettingsAsync(
                        global::Grid.Bot.Properties.Settings.Default.ClientSettingsApiKey
                    );

                    await command.RespondPublicAsync(
                        "Successfully refreshed all applications!"
                    );

                    return;
            }
        }
    }
}

#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

#endif
