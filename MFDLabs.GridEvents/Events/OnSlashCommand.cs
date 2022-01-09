#if WE_LOVE_EM_SLASH_COMMANDS

using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Registries;

namespace MFDLabs.Grid.Bot.Events
{
    public static class OnSlashCommand
    {
        public static async Task Invoke(SocketSlashCommand command)
        {
            await command.User.FireEventAsync(typeof(OnMessage).FullName, command.Channel.Name);

            var userIsAdmin = command.User.IsAdmin();
            var userIsPrivilaged = command.User.IsPrivilaged();

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
                    var failureMessage = global::MFDLabs.Grid.Bot.Properties.Settings.Default.ReasonForDying;

                    if (failureMessage != null) await command.RespondEphemeralAsync(failureMessage);

                    return;
                }
            }

            await CommandRegistry.CheckAndRunSlashCommand(command);
        }
    }
}

#endif // WE_LOVE_EM_SLASH_COMMANDS