namespace Grid.Bot.WorkQueues;

using System;
using System.Linq;
using System.Threading.Tasks;

using Discord.WebSocket;

using Logging;

using Text.Extensions;

using Global;
using Utility;
using Registries;
using Extensions;

internal sealed class OnMessageReceivedWorkQueue : AsyncWorkQueue<SocketMessage>
{
    public static readonly OnMessageReceivedWorkQueue Singleton = new();

    public OnMessageReceivedWorkQueue()
        : base(OnReceive)
    { }

    private static async void OnReceive(SocketMessage message)
    {
        if (!BotRegistry.Ready) return; // We do not want to process if not ready, this is crucial!

        var userIsAdmin = message.Author.IsAdmin();
        var userIsPrivilaged = message.Author.IsPrivilaged();
        var userIsBlacklisted = message.Author.IsBlacklisted();

        if (message.Author.IsBot) return;

        var messageContent = message.Content;

        if (!ParsePrefix(ref messageContent)) return;

        if (MaintenanceSettings.Singleton.MaintenanceEnabled)
        {
            if (!userIsAdmin && !userIsPrivilaged)
            {
                Logger.Singleton.Warning("Maintenance enabled, and someone tried to use it!!");

                var failureMessage = MaintenanceSettings.Singleton.MaintenanceStatus;

                if (!failureMessage.IsNullOrEmpty()) await message.ReplyAsync(failureMessage);

                return;
            }
        }

        if (userIsBlacklisted)
        {
            Logger.Singleton.Warning(
                "A blacklisted user {0}('{1}#{2}') tried to use the bot, attempt to DM that they are blacklisted.", 
                message.Author.Id, 
                message.Author.Username,
                message.Author.Discriminator
            );

            try
            {
                await message.Author.SendDirectMessageAsync($"you are unable to use this bot as you've been blacklisted, to have your case reviewed, please refer to https://grid-bot.ops.vmminfra.net/moderation#appealing-blacklisting for more information.");
            }
            catch
            {
                Logger.Singleton.Warning("We tried to DM the user, but their DMs may not be available.");
            }

            return;
        }

        if (messageContent.ToLower().Contains("@everyone") || messageContent.ToLower().Contains("@here") && !userIsAdmin)
            return;

        var messageContentArray = GetContentArray(messageContent);

        await HandleCommand(messageContentArray, message);
    }

    private static async Task HandleCommand(string[] messageContent, SocketMessage message)
    {
        // there is an issue here when parsing newlines, it will take all of the command and newline if `;command\nargs` is present as an entire command name
        // todo: try to remove newlines from this a much as we can, we can also try parsing the args by removing $`{command}\n` + $`{command}\r\n` ¯\_(ツ)_/¯
        // note: may have fixed it for now
        var alias = messageContent[0].ToLower().Trim();
        var contentArray = messageContent.Skip(1).Take(messageContent.Length - 1).ToArray();

        if (alias.Contains('\n'))
        {
            alias = alias.Split('\n')[0];
            messageContent[0] = messageContent[0].Replace($"{alias}\n", "");
            contentArray = messageContent;
        }


        await CommandRegistry.CheckAndRunCommandByAlias(alias, contentArray, message);
    }

    private static string[] GetContentArray(string messageContent)
    {
        return messageContent.Contains(" ") ? messageContent.Split(' ') : new[] { messageContent };
    }

    private static bool ParsePrefix(ref string message)
    {
        if (!message.StartsWith(CommandsSettings.Singleton.Prefix))
        {
            return false;
        }

        message = message.Substring(CommandsSettings.Singleton.Prefix.Length);

        return true;
    }
}
