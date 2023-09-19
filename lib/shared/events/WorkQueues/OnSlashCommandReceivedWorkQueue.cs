#if WE_LOVE_EM_SLASH_COMMANDS

namespace Grid.Bot.WorkQueues;

using Discord.WebSocket;

using Logging;

using Text.Extensions;

using Global;
using Utility;
using Registries;
using Extensions;

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

            if (command.User.IsBot) return;

            if (MaintenanceSettings.Singleton.MaintenanceEnabled)
            {
                if (!userIsAdmin && !userIsPrivilaged)
                {
                    Logger.Singleton.Warning("Maintenance enabled, and someone tried to use it!!");

                    var failureMessage = MaintenanceSettings.Singleton.MaintenanceStatus;

                    if (!failureMessage.IsNullOrEmpty())
                        await command.RespondEphemeralPingAsync(failureMessage);

                    return;
                }
            }

            if (userIsBlacklisted)
            {
                Logger.Singleton.Warning(
                    "A blacklisted user {0}('{1}#{2}') tried to use the bot.", 
                    command.User.Id,
                    command.User.Username,
                    command.User.Discriminator
                );

                await command.RespondEphemeralPingAsync("you are unable to use this bot as you've been blacklisted, to have your case reviewed, please refer to https://grid-bot.ops.vmminfra.net/moderation#appealing-blacklisting for more information.");

                return;
            }

            await CommandRegistry.CheckAndRunSlashCommand(command);
        }
    }
}

#endif
