#if WE_LOVE_EM_SLASH_COMMANDS

using Microsoft.Ccr.Core;
using Discord.WebSocket;
using MFDLabs.Logging;
using MFDLabs.Concurrency;
using MFDLabs.Text.Extensions;
using MFDLabs.Grid.Bot.Utility;
using MFDLabs.Grid.Bot.Registries;
using MFDLabs.Grid.Bot.Extensions;

namespace MFDLabs.Grid.Bot.WorkQueues
{
    internal sealed class OnSlashCommandReceivedWorkQueue : AsyncWorkQueue<SocketSlashCommand>
    {
        private static readonly DispatcherQueue _DispatcherQueue = new PatchedDispatcherQueue("On Slash Command Received Work Queue", new(0, "On Slash Command Received Work Queue Dispatcher"));

        public static readonly OnSlashCommandReceivedWorkQueue Singleton = new();

        public OnSlashCommandReceivedWorkQueue()
            : base(_DispatcherQueue, OnReceive)
        { }

        private static async void OnReceive(SocketSlashCommand command, SuccessFailurePort result)
        {
            await command.User.FireEventAsync(typeof(OnSlashCommandReceivedWorkQueue).FullName, command.Channel.Name);

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

#endif