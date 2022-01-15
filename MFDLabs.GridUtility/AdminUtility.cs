using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Properties;
using MFDLabs.Logging;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Grid.Bot.Utility
{
    internal static class ReplyExtensions
    {
        public static async Task<RestUserMessage> ReplyAsync(
            this SocketMessage message,
            string text = null,
            bool isTts = false,
            Embed embed = null,
            RequestOptions options = null,
            MessageReference messageReference = null)
            => await message.Channel.SendMessageAsync(
                    $"<@{message.Author.Id}>{(!text.IsNullOrEmpty() ? ", " : "")}{text}",
                    isTts,
                    embed,
                    options,
                    new AllowedMentions(AllowedMentionTypes.Users),
                    messageReference
                );
    }
    
    public static class AdminUtility
    {
        private static IEnumerable<string> AllowedChannelIDs =>
            (from id in global::MFDLabs.Grid.Bot.Properties.Settings.Default.AllowedChannels.Split(',')
                where !id.IsNullOrEmpty()
                select id).ToArray();

        private static IEnumerable<string> AdministratorUserIDs =>
            (from id in global::MFDLabs.Grid.Bot.Properties.Settings.Default.Admins.Split(',')
                where !id.IsNullOrEmpty()
                select id).ToArray();

        private static IEnumerable<string> HigherPrivilagedUserIDs =>
            (from id in global::MFDLabs.Grid.Bot.Properties.Settings.Default.HigherPrivilagedUsers.Split(',')
                where !id.IsNullOrEmpty()
                select id).ToArray();

        public static bool UserIsOwner(IUser user) => UserIsOwner(user.Id);

        public static bool UserIsOwner(ulong id) => id == global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotOwnerID;

        public static bool UserIsAdmin(IUser user) => UserIsAdmin(user.Id);

        public static bool UserIsAdmin(ulong id) => UserIsAdmin(id.ToString());

        public static bool UserIsAdmin(string id) => UserIsOwner(ulong.Parse(id)) || AdministratorUserIDs.Contains(id);

        public static bool UserIsPrivilaged(IUser user) => UserIsPrivilaged(user.Id);

        public static bool UserIsPrivilaged(ulong id) => UserIsPrivilaged(id.ToString());

        public static bool UserIsPrivilaged(string id) => UserIsAdmin(id) || HigherPrivilagedUserIDs.Contains(id);

        public static bool ChannelIsAllowed(IChannel channel) => ChannelIsAllowed(channel.Id);

        public static bool ChannelIsAllowed(ulong id) => ChannelIsAllowed(id.ToString());

        public static bool ChannelIsAllowed(string id) => AllowedChannelIDs.Contains(id);

        public static bool ChannelIsAllowed(this SocketMessage message)
        {
            var allowedChannelIds = message.GetSetting<string>("AllowedChannels").Split(',');
            return allowedChannelIds.Contains(message.Channel.Id.ToString());
        }

        public static async Task<bool> RejectIfNotPrivilagedAsync(SocketMessage message)
        {
            var isPrivilaged = UserIsPrivilaged(message.Author);

            if (!isPrivilaged)
            {
                SystemLogger.Singleton.Warning("User '{0}' is not on the admin whitelist or the privilaged users " +
                                               "list. Please take this with caution as leaked internal methods may be abused!", message.Author.Id);
                await message.ReplyAsync("Only privilaged users or administrators can execute that command.");
                return false;
            }

            SystemLogger.Singleton.Info("User '{0}' is privilaged or an admin.", message.Author.Id);
            return true;
        }

        public static async Task<bool> RejectIfNotAdminAsync(SocketMessage message)
        {
            if (!UserIsAdmin(message.Author))
            {
                SystemLogger.Singleton.Warning("User '{0}' is not on the admin whitelist. Please take this with " +
                                               "caution as leaked internal methods may be abused!", message.Author.Id);
                await message.ReplyAsync("You lack the correct permissions to execute that command.");
                return false;
            }
            SystemLogger.Singleton.Info("User '{0}' is on the admin whitelist.", message.Author.Id);
            return true;
        }

        public static async Task<bool> RejectIfNotOwnerAsync(SocketMessage message)
        {
            if (!UserIsOwner(message.Author))
            {
                SystemLogger.Singleton.Warning("User '{0}' is not the owner. Please take this with caution " +
                                               "as leaked internal methods may be abused!", message.Author.Id);
                await message.ReplyAsync("You lack the correct permissions to execute that command.");
                return false;
            }
            SystemLogger.Singleton.Info("User '{0}' is the owner.", message.Author.Id);
            return true;
        }
    }
}
