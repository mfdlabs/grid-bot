using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Properties;
using MFDLabs.Grid.Bot.Registries;
using MFDLabs.Grid.Bot.Utility;
using MFDLabs.Logging;

namespace MFDLabs.Grid.Bot.Events
{
    public static class OnMessage
    {
        public static async Task Invoke(SocketMessage message)
        {
            await message.Author.FireEventAsync(typeof(OnMessage).FullName, message.Channel.Name);
            await message.Author.PageViewedAsync($"{typeof(OnMessage).FullName}({message.Channel.Name})");

            var userIsAdmin = message.Author.IsAdmin();
            var userIsPrivilaged = message.Author.IsPrivilaged();

            if (message.Author.IsBot && !global::MFDLabs.Grid.Bot.Properties.Settings.Default.AllowParsingForBots) return;

            if (!message.GetSetting<bool>("AllowAllChannels"))
            {
                if (!message.ChannelIsAllowed() && !userIsAdmin)
                    return;
            }

            var messageContent = message.Content;

            if (!ParsePrefix(ref messageContent)) return;

            if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.IsEnabled)
            {
                if (!userIsAdmin && !userIsPrivilaged)
                {
                    SystemLogger.Singleton.Warning("Maintenance enabled, and someone tried to use it!!");

                    var failureMessage = global::MFDLabs.Grid.Bot.Properties.Settings.Default.ReasonForDying;

                    if (failureMessage != null) await message.ReplyAsync(failureMessage);

                    return;
                }
            }

            if (messageContent.ToLower().Contains("@everyone") || messageContent.ToLower().Contains("@here") && !userIsAdmin)
            {
                await message.Author.FireEventAsync("Fatality", "They tried to ping @everyone or @here");
                await message.ReplyAsync("You are unable to use the following mentions in your command.");
                return;
            }

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
            if (!message.StartsWith(global::MFDLabs.Grid.Bot.Properties.Settings.Default.Prefix))
            {
                return false;
            }

            message = message.Substring(global::MFDLabs.Grid.Bot.Properties.Settings.Default.Prefix.Length);

            return true;
        }
    }
}
