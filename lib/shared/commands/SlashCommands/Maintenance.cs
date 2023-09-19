#if WE_LOVE_EM_SLASH_COMMANDS

namespace Grid.Bot.SlashCommands;

using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using Text.Extensions;

using Global;
using Extensions;
using Interfaces;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

/// <summary>
/// Manages interacting with maintenance methods.
/// </summary>
internal class Maintenance : ISlashCommandHandler
{
    /// <inheritdoc cref="ISlashCommandHandler.Description"/>
    public string Description => "Enables or disables Maintenance";

    /// <inheritdoc cref="ISlashCommandHandler.Name"/>
    public string Name => "maintenance";

    /// <inheritdoc cref="ISlashCommandHandler.IsInternal"/>
    public bool IsInternal => true;

    /// <inheritdoc cref="ISlashCommandHandler.IsEnabled"/>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc cref="ISlashCommandHandler.Options"/>
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
        => updateText.IsNullOrEmpty() ? "Maintenance is enabled" : $"Maintenance is enabled: {updateText}";

    /// <inheritdoc cref="ISlashCommandHandler.ExecuteAsync(SocketSlashCommand)"/>
    public async Task ExecuteAsync(SocketSlashCommand command)
    {
        if (!await command.RejectIfNotAdminAsync()) return;

        var subCommand = command.Data.GetSubCommand();
        var option = subCommand.Name;

        switch (option)
        {
            case "enable":
                var optionalMessage = subCommand.GetOptionValue<string>("status_text");

                if (MaintenanceSettings.Singleton.MaintenanceEnabled)
                {
                    if (!optionalMessage.IsNullOrEmpty() && optionalMessage != MaintenanceSettings.Singleton.MaintenanceStatus)
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
                    optionalMessage = MaintenanceSettings.Singleton.MaintenanceStatus;

                MaintenanceSettings.Singleton.MaintenanceEnabled = true;

                BotRegistry.Client.SetStatusAsync(UserStatus.DoNotDisturb);
                BotRegistry.Client.SetGameAsync(GetStatusText(optionalMessage));

                if (!optionalMessage.IsNullOrEmpty() && optionalMessage != MaintenanceSettings.Singleton.MaintenanceStatus)
                    MaintenanceSettings.Singleton.MaintenanceStatus = optionalMessage;

                await command.RespondEphemeralAsync("Successfully enabled the maintenance status with the optional message of " +
                                         $"'{(optionalMessage.IsNullOrEmpty() ? "No Message" : optionalMessage)}'!");

                return;
            case "disable":
                if (!MaintenanceSettings.Singleton.MaintenanceEnabled)
                {
                    await command.RespondEphemeralAsync("The maintenance status is not enabled! " +
                                             "if you want to enable it, please re-run the command like: " +
                                             $"'/maintenance enable status_text:optionalMessage?'");
                    return;
                }

                MaintenanceSettings.Singleton.MaintenanceEnabled = false;

                BotRegistry.Client.SetStatusAsync(DiscordSettings.Singleton.BotStatus);

                if (!DiscordSettings.Singleton.BotStatusMessage.IsNullOrEmpty())
                    BotRegistry.Client.SetGameAsync(DiscordSettings.Singleton.BotStatusMessage);

                await command.RespondEphemeralAsync("Successfully disabled the maintenance status!");

                return;
            case "update":
                if (!MaintenanceSettings.Singleton.MaintenanceEnabled)
                {
                    await command.RespondEphemeralAsync("The maintenance status is not enabled! " +
                                             "if you want to enable it, please re-run the command like: " +
                                             $"'/maintenance enable status_text:optionalMessage?'");
                    return;
                }

                var oldMessage = MaintenanceSettings.Singleton.MaintenanceStatus;

                var optionalMessageForUpdate = subCommand.GetOptionValue<string>("status_text");
                if (optionalMessageForUpdate.IsNullOrEmpty()) optionalMessageForUpdate = "";

                if (oldMessage == optionalMessageForUpdate)
                {
                    await command.RespondEphemeralAsync("Not updating maintenance status, the new message matches the old status.");

                    return;
                }

                MaintenanceSettings.Singleton.MaintenanceStatus = optionalMessageForUpdate;

                BotRegistry.Client.SetGameAsync(GetStatusText(optionalMessageForUpdate));

                await command.RespondEphemeralAsync($"Successfully updated the maintenance status text from '{oldMessage}' to " +
                                        $"'{(optionalMessageForUpdate.IsNullOrEmpty() ? "No Message" : optionalMessageForUpdate)}'!");

                return;
            case "delete":
                if (MaintenanceSettings.Singleton.MaintenanceStatus.IsNullOrEmpty())
                {
                    await command.RespondEphemeralAsync("The maintenance text is already empty!");

                    return;
                }

                // This only removes the maintenance text
                MaintenanceSettings.Singleton.MaintenanceStatus = string.Empty;

                if (MaintenanceSettings.Singleton.MaintenanceEnabled)
                   BotRegistry.Client.SetGameAsync(GetStatusText(null), null, ActivityType.Playing);

                await command.RespondEphemeralAsync("Sucessfully removed the maintenance text!");

                return;

            case "get":
                var deathMessage = MaintenanceSettings.Singleton.MaintenanceStatus;

                var embed = new EmbedBuilder()
                    .WithTitle("Maintenance Status")
                    .WithCurrentTimestamp()
                    .AddField(
                        "Maintenance Is Enabled?",
                        !MaintenanceSettings.Singleton.MaintenanceEnabled ? "No" : "Yes",
                        true
                    )
                    .AddField(
                        "Maintenance Status Message",
                        deathMessage.IsNullOrEmpty() ? "No Message!" : deathMessage,
                        true
                    );

                if (!MaintenanceSettings.Singleton.MaintenanceEnabled)
                    embed.WithColor(0x00, 0xff, 0x00);
                else
                    embed.WithColor(0xff, 0x00, 0x00);

                await command.RespondEphemeralAsync(embed: embed.Build());

                return;
        }
    }
}

#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

#endif
