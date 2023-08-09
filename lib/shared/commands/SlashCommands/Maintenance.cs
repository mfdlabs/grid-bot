#if WE_LOVE_EM_SLASH_COMMANDS

using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Grid.Bot.Extensions;
using Grid.Bot.Interfaces;
using Grid.Bot.Utility;
using Text.Extensions;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

namespace Grid.Bot.SlashCommands
{
    internal class Maintenance : IStateSpecificSlashCommandHandler
    {
        public string CommandDescription => "Enables or disables Maintenance";
        public string CommandAlias => "maintenance";
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;
        public ulong? GuildId => null;
        public SlashCommandOptionBuilder[] Options => new[]
        {
            new SlashCommandOptionBuilder()
                .WithName("enable")
                .WithDescription("Enables the maintenance status")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("status_text", ApplicationCommandOptionType.String, "Optional status text", false),
            new SlashCommandOptionBuilder()
                .WithName("disable")
                .WithDescription("Disables the current maintenance status")
                .WithType(ApplicationCommandOptionType.SubCommand),
            new SlashCommandOptionBuilder()
                .WithName("get")
                .WithDescription("Gets the current maintenance status.")
                .WithType(ApplicationCommandOptionType.SubCommand),
            new SlashCommandOptionBuilder()
                .WithName("delete")
                .WithDescription("Deletes the current maintenance status text")
                .WithType(ApplicationCommandOptionType.SubCommand),
            new SlashCommandOptionBuilder()
                .WithName("update")
                .WithDescription("Updates the current maintenance text")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("status_text", ApplicationCommandOptionType.String, "Status text, cannot be length 0", true)
        };

        private string GetStatusText(string updateText)
        {
            return updateText.IsNullOrEmpty() ? "Maintenance is enabled" : $"Maintenance is enabled: {updateText}";
        }

        public async Task Invoke(SocketSlashCommand command)
        {
            if (!await command.RejectIfNotAdminAsync()) return;

            var subCommand = command.Data.GetSubCommand();
            var option = subCommand.Name;

            switch (option)
            {
                case "enable":
                    var optionalMessage = subCommand.GetOptionValue("status_text")?.ToString();

                    if (!global::Grid.Bot.Properties.Settings.Default.IsEnabled)
                    {
                        if (!optionalMessage.IsNullOrEmpty() && optionalMessage != global::Grid.Bot.Properties.Settings.Default.ReasonForDying)
                        {
                            await command.RespondEphemeralAsync("The maintenance status is already enabled, and it appears you have a different message, " +
                                                    "if you want to update the exsting message, please re-run the command like: " +
                                                    $"'/maintenance update `status_text:{optionalMessage}`'");
                            return;
                        }
                        await command.RespondEphemeralAsync("The maintenance status is already enabled!");
                        return;
                    }

                    if (optionalMessage.IsNullOrEmpty())
                        optionalMessage = global::Grid.Bot.Properties.Settings.Default.ReasonForDying;

                    global::Grid.Bot.Properties.Settings.Default["IsEnabled"] = false;

                    global::Grid.Bot.Global.BotRegistry.Client.SetStatusAsync(UserStatus.DoNotDisturb);
                    global::Grid.Bot.Global.BotRegistry.Client.SetGameAsync(
                        GetStatusText(optionalMessage),
                        null,
                        ActivityType.Playing
                    );

                    if (!optionalMessage.IsNullOrEmpty() && optionalMessage != global::Grid.Bot.Properties.Settings.Default.ReasonForDying)
                        global::Grid.Bot.Properties.Settings.Default["ReasonForDying"] = optionalMessage;


                    global::Grid.Bot.Properties.Settings.Default.Save();

                    await command.RespondEphemeralAsync("Successfully enabled the maintenance status with the optional message of " +
                                             $"'{(optionalMessage.IsNullOrEmpty() ? "No Message" : optionalMessage)}'!");

                    return;
                case "disable":
                    if (global::Grid.Bot.Properties.Settings.Default.IsEnabled)
                    {
                        await command.RespondEphemeralAsync("The maintenance status is not enabled! " +
                                                 "if you want to enable it, please re-run the command like: " +
                                                 $"'/maintenance enable status_text:optionalMessage?'");
                        return;
                    }

                    global::Grid.Bot.Properties.Settings.Default["IsEnabled"] = true;
                    global::Grid.Bot.Properties.Settings.Default.Save();

                    global::Grid.Bot.Global.BotRegistry.Client.SetStatusAsync(
                        global::Grid.Bot.Properties.Settings.Default.BotGlobalUserStatus
                    );

                    if (!global::Grid.Bot.Properties.Settings.Default.BotGlobalStatusMessage.IsNullOrEmpty())
                        global::Grid.Bot.Global.BotRegistry.Client.SetGameAsync(
                            global::Grid.Bot.Properties.Settings.Default.BotGlobalStatusMessage,
                            global::Grid.Bot.Properties.Settings.Default.BotGlobalStreamURL,
                            global::Grid.Bot.Properties.Settings.Default.BotGlobalActivityType
                        );

                    await command.RespondEphemeralAsync("Successfully disabled the maintenance status!");

                    return;
                case "update":
                    if (global::Grid.Bot.Properties.Settings.Default.IsEnabled)
                    {
                        await command.RespondEphemeralAsync("The maintenance status is not enabled! " +
                                                 "if you want to enable it, please re-run the command like: " +
                                                 $"'/maintenance enable status_text:optionalMessage?'");
                        return;
                    }

                    var oldMessage = global::Grid.Bot.Properties.Settings.Default.ReasonForDying;

                    var optionalMessageForUpdate = subCommand.GetOptionValue("status_text")?.ToString();
                    if (optionalMessageForUpdate.IsNullOrEmpty()) optionalMessageForUpdate = "";

                    if (oldMessage == optionalMessageForUpdate)
                    {
                        await command.RespondEphemeralAsync("Not updating maintenance status, the new message matches the old status.");
                        return;
                    }

                    global::Grid.Bot.Properties.Settings.Default["ReasonForDying"] = optionalMessageForUpdate;
                    global::Grid.Bot.Properties.Settings.Default.Save();

                    global::Grid.Bot.Global.BotRegistry.Client.SetGameAsync(
                        GetStatusText(optionalMessageForUpdate),
                        null,
                        ActivityType.Playing
                    );

                    await command.RespondEphemeralAsync($"Successfully updated the maintenance status text from '{oldMessage}' to " +
                                            $"'{(optionalMessageForUpdate.IsNullOrEmpty() ? "No Message" : optionalMessageForUpdate)}'!");

                    return;
                case "delete":
                    if (global::Grid.Bot.Properties.Settings.Default.ReasonForDying.IsNullOrEmpty())
                    {
                        await command.RespondEphemeralAsync("The maintenance text is already empty!");
                        return;
                    }

                    // This only removes the maintenance text
                    global::Grid.Bot.Properties.Settings.Default["ReasonForDying"] = string.Empty;
                    global::Grid.Bot.Properties.Settings.Default.Save();

                    if (!global::Grid.Bot.Properties.Settings.Default.IsEnabled)
                        global::Grid.Bot.Global.BotRegistry.Client.SetGameAsync(GetStatusText(null), null, ActivityType.Playing);

                    await command.RespondEphemeralAsync("Sucessfully removed the maintenance text!");

                    return;

                case "get":
                    var deathMessage = global::Grid.Bot.Properties.Settings.Default.ReasonForDying;

                    var embed = new EmbedBuilder()
                        .WithTitle("Maintenance Status")
                        .WithCurrentTimestamp()
                        .AddField(
                            "Maintenance Is Enabled?",
                            global::Grid.Bot.Properties.Settings.Default.IsEnabled ? "No" : "Yes",
                            true
                        )
                        .AddField(
                            "Maintenance Status Message",
                            deathMessage.IsNullOrEmpty() ? "No Message!" : deathMessage,
                            true
                        );

                    if (global::Grid.Bot.Properties.Settings.Default.IsEnabled)
                        embed.WithColor(0x00, 0xff, 0x00);
                    else
                        embed.WithColor(0xff, 0x00, 0x00);

                    await command.RespondEphemeralAsync(embed: embed.Build());
                    return;
            }
        }
    }
}

#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

#endif