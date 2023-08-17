#if WE_LOVE_EM_SLASH_COMMANDS

using Discord.WebSocket;

using Logging;

using Text.Extensions;
using Grid.Bot.Global;
using Grid.Bot.Utility;
using Grid.Bot.Registries;
using Grid.Bot.Extensions;

namespace Grid.Bot.WorkQueues
{
    internal sealed class OnSlashCommandReceivedWorkQueue : AsyncWorkQueue<SocketSlashCommand>
    {
        public static readonly OnSlashCommandReceivedWorkQueue Singleton = new();

        public OnSlashCommandReceivedWorkQueue()
            : base(OnReceive)
        { }

        private static async void OnReceive(SocketSlashCommand command)
        {
            if (!BotRegistry.Ready) return; // We do not want to process if not ready, this is crucial!

            using (await command.DeferPublicAsync())
            {
                var userIsAdmin = command.User.IsAdmin();
                var userIsPrivilaged = command.User.IsPrivilaged();
                var userIsBlacklisted = command.User.IsBlacklisted();

                if (command.User.IsBot && !global::Grid.Bot.Properties.Settings.Default.AllowParsingForBots) return;

                if (!global::Grid.Bot.Properties.Settings.Default.AllowAllChannels)
                {
                    if (!command.Channel.IsWhitelisted() && !userIsAdmin)
                        return;
                }

                if (!global::Grid.Bot.Properties.Settings.Default.IsEnabled)
                {
                    if (!userIsAdmin && !userIsPrivilaged)
                    {
                        Logger.Singleton.Warning("Maintenance enabled, and someone tried to use it!!");

                        var failureMessage = global::Grid.Bot.Properties.Settings.Default.ReasonForDying;

                        if (!failureMessage.IsNullOrEmpty()) await command.RespondEphemeralPingAsync(failureMessage);

                        return;
                    }
                }

                if (userIsBlacklisted)
                {
                    Logger.Singleton.Warning("A blacklisted user {0}('{1}#{2}') tried to use the bot.", command.User.Id, command.User.Username, command.User.Discriminator);

                    await command.RespondEphemeralPingAsync("you are unable to use this bot as you've been blacklisted, to have your case reviewed, please refer to https://grid-bot.ops.vmminfra.net/moderation#appealing-blacklisting for more information.");

                    return;
                }

                await CommandRegistry.CheckAndRunSlashCommand(command);
            }
        }
    }
}

#endif
