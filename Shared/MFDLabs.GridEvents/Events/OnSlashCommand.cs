#if WE_LOVE_EM_SLASH_COMMANDS

using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Registries;
using MFDLabs.Text.Extensions;
using MFDLabs.Logging;

namespace MFDLabs.Grid.Bot.Events
{
    public static class OnSlashCommand
    {
        public static async Task Invoke(SocketSlashCommand command)
        {
            await command.User.FireEventAsync(typeof(OnMessage).FullName, command.Channel.Name);

            var userIsAdmin = command.User.IsAdmin();
            var userIsPrivilaged = command.User.IsPrivilaged();
            var userIsBlacklisted = command.User.IsBlacklisted();

            if (command.User.IsBot && !global::MFDLabs.Grid.Bot.Properties.Settings.Default.AllowParsingForBots) return;

            if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.AllowAllChannels)
            {
                if (!command.Channel.IsWhitelisted() && !userIsAdmin)
                    return;
            }

            if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.IsEnabled)
            {
                if (!userIsAdmin && !userIsPrivilaged)
                {
                    SystemLogger.Singleton.Warning("Maintenance enabled, and someone tried to use it!!");

                    var failureMessage = global::MFDLabs.Grid.Bot.Properties.Settings.Default.ReasonForDying;

                    if (!failureMessage.IsNullOrEmpty()) await command.RespondEphemeralPingAsync(failureMessage);

                    return;
                }
            }

            if (userIsBlacklisted)
            {
                SystemLogger.Singleton.Warning("A blacklisted user {0}('{1}#{2}') tried to use the bot, attempt to DM that they are blacklisted.", command.User.Id, command.User.Username, command.User.Discriminator);

                try
                {
                    await command.User.SendDirectMessageAsync($"you are unable to use this bot as you've been blacklisted, to have your case reviewed, please contact <@{(global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotOwnerID)}>");
                }
                catch
                {
                    SystemLogger.Singleton.Warning("We tried to DM the user, but their DMs may not be available.");
                }

                return;
            }

            await CommandRegistry.CheckAndRunSlashCommand(command);
        }
    }
}

#endif // WE_LOVE_EM_SLASH_COMMANDS