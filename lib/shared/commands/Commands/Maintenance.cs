using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;
using MFDLabs.Text.Extensions;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

namespace MFDLabs.Grid.Bot.Commands
{
    internal class Maintenance : IStateSpecificCommandHandler
    {
        public string CommandName => "Maintenance";
        public string CommandDescription => "Enables or disables Maintenance, update -> if the optional message is empty then it will remove the current one.";
        public string[] CommandAliases => new[] { "maint", "maintenance" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        private string GetStatusText(string updateText)
        {
            return updateText.IsNullOrEmpty() ? "Maintenance is enabled" : $"Maintenance is enabled: {updateText}";
        }

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            var option = messageContentArray.ElementAtOrDefault(0);

            if (option.IsNullOrEmpty())
            {
                await message.ReplyAsync("Expected the option to either be 'enable', 'disable', 'get', 'delete' or 'update'");
                return;
            }


            switch (option.ToLower())
            {
                case "enable":
                    var optionalMessage = messageContentArray.Skip(1).Join(' ');

                    if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.IsEnabled)
                    {
                        if (!optionalMessage.IsNullOrEmpty() && optionalMessage != global::MFDLabs.Grid.Bot.Properties.Settings.Default.ReasonForDying)
                        {
                            await message.ReplyAsync("The maintenance status is already enabled, and it appears you have a different message, " +
                                                    "if you want to update the exsting message, please re-run the command like: " +
                                                    $"'{(global::MFDLabs.Grid.Bot.Properties.Settings.Default.Prefix)}{originalCommand} update {optionalMessage}'");
                            return;
                        }
                        await message.ReplyAsync("The maintenance status is already enabled!");
                        return;
                    }

                    if (optionalMessage.IsNullOrEmpty())
                        optionalMessage = global::MFDLabs.Grid.Bot.Properties.Settings.Default.ReasonForDying;

                    global::MFDLabs.Grid.Bot.Properties.Settings.Default["IsEnabled"] = false;

                    global::MFDLabs.Grid.Bot.Global.BotRegistry.Client.SetStatusAsync(UserStatus.DoNotDisturb);
                    global::MFDLabs.Grid.Bot.Global.BotRegistry.Client.SetGameAsync(
                        GetStatusText(optionalMessage),
                        null,
                        ActivityType.Playing
                    );

                    if (!optionalMessage.IsNullOrEmpty() && optionalMessage != global::MFDLabs.Grid.Bot.Properties.Settings.Default.ReasonForDying)
                        global::MFDLabs.Grid.Bot.Properties.Settings.Default["ReasonForDying"] = optionalMessage;


                    global::MFDLabs.Grid.Bot.Properties.Settings.Default.Save();

                    await message.ReplyAsync("Successfully enabled the maintenance status with the optional message of " +
                                             $"'{(optionalMessage.IsNullOrEmpty() ? "No Message" : optionalMessage)}'!");

                    return;
                case "disable":
                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.IsEnabled)
                    {
                        await message.ReplyAsync("The maintenance status is not enabled! " +
                                                 "if you want to enable it, please re-run the command like: " +
                                                 $"'{(global::MFDLabs.Grid.Bot.Properties.Settings.Default.Prefix)}{originalCommand} enable optionalMessage?'");
                        return;
                    }

                    global::MFDLabs.Grid.Bot.Properties.Settings.Default["IsEnabled"] = true;
                    global::MFDLabs.Grid.Bot.Properties.Settings.Default.Save();

                    global::MFDLabs.Grid.Bot.Global.BotRegistry.Client.SetStatusAsync(
                        global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotGlobalUserStatus
                    );

                    if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotGlobalStatusMessage.IsNullOrEmpty())
                        global::MFDLabs.Grid.Bot.Global.BotRegistry.Client.SetGameAsync(
                            global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotGlobalStatusMessage,
                            global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotGlobalStreamURL,
                            global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotGlobalActivityType
                        );

                    await message.ReplyAsync("Successfully disabled the maintenance status!");

                    return;
                case "update":
                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.IsEnabled)
                    {
                        await message.ReplyAsync("The maintenance status is not enabled! " +
                                                 "if you want to enable it, please re-run the command like: " +
                                                 $"'{(global::MFDLabs.Grid.Bot.Properties.Settings.Default.Prefix)}{originalCommand} enable optionalMessage?'");
                        return;
                    }

                    var oldMessage = global::MFDLabs.Grid.Bot.Properties.Settings.Default.ReasonForDying;

                    var optionalMessageForUpdate = messageContentArray.Skip(1).Join(' ');
                    if (optionalMessageForUpdate.IsNullOrEmpty()) optionalMessageForUpdate = "";

                    if (oldMessage == optionalMessageForUpdate)
                    {
                        await message.ReplyAsync("Not updating maintenance status, the new message matches the old status.");
                        return;
                    }

                    global::MFDLabs.Grid.Bot.Properties.Settings.Default["ReasonForDying"] = optionalMessageForUpdate;
                    global::MFDLabs.Grid.Bot.Properties.Settings.Default.Save();

                    global::MFDLabs.Grid.Bot.Global.BotRegistry.Client.SetGameAsync(
                        GetStatusText(optionalMessageForUpdate),
                        null,
                        ActivityType.Playing
                    );

                    await message.ReplyAsync($"Successfully updated the maintenance status text from '{oldMessage}' to " +
                                            $"'{(optionalMessageForUpdate.IsNullOrEmpty() ? "No Message" : optionalMessageForUpdate)}'!");

                    return;
                case "delete":
                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.ReasonForDying.IsNullOrEmpty())
                    {
                        await message.ReplyAsync("The maintenance text is already empty!");
                        return;
                    }

                    // This only removes the maintenance text
                    global::MFDLabs.Grid.Bot.Properties.Settings.Default["ReasonForDying"] = string.Empty;
                    global::MFDLabs.Grid.Bot.Properties.Settings.Default.Save();

                    if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.IsEnabled)
                        global::MFDLabs.Grid.Bot.Global.BotRegistry.Client.SetGameAsync(GetStatusText(null), null, ActivityType.Playing);

                    await message.ReplyAsync("Sucessfully removed the maintenance text!");

                    return;

                case "get":
                    var deathMessage = global::MFDLabs.Grid.Bot.Properties.Settings.Default.ReasonForDying;

                    var embed = new EmbedBuilder()
                        .WithTitle("Maintenance Status")
                        .WithCurrentTimestamp()
                        .AddField(
                            "Maintenance Is Enabled?",
                            global::MFDLabs.Grid.Bot.Properties.Settings.Default.IsEnabled ? "No" : "Yes",
                            true
                        )
                        .AddField(
                            "Maintenance Status Message",
                            deathMessage.IsNullOrEmpty() ? "No Message!" : deathMessage,
                            true
                        );

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.IsEnabled)
                        embed.WithColor(0x00, 0xff, 0x00);
                    else
                        embed.WithColor(0xff, 0x00, 0x00);

                    await message.ReplyAsync(embed: embed.Build());
                    return;
                default:
                    await message.ReplyAsync($"Unkown option '{option.ToLower()}'. Expected the option to either be 'enable', 'disable', 'get', 'delete' or 'update'");
                    return;
            }
        }
    }
}

#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed