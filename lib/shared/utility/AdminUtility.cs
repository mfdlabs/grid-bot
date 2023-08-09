using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.Rest;
using Discord.WebSocket;

using Logging;

using Text.Extensions;
using Grid.Bot.Properties;

namespace Grid.Bot.Utility
{
    internal static class ReplyExtensions
    {
        public static async Task<RestUserMessage> ReplyAsync(
            this SocketMessage message,
            string text = null,
            bool isTts = false,
            Embed embed = null,
            RequestOptions options = null
        ) => await message.Channel.SendMessageAsync(
                    text,
                    isTts,
                    embed,
                    options,
                    new AllowedMentions(AllowedMentionTypes.Users) { MentionRepliedUser = true },
                    new MessageReference(message.Id)
                );

        public static async Task RespondEphemeralAsync(
            this SocketCommandBase command,
            string text = null,
            bool pingUser = false,
            Embed[] embeds = null,
            bool isTts = false,
            MessageComponent component = null,
            Embed embed = null,
            RequestOptions options = null
        )
        {
            if (!command.HasResponded)
            {
                await command.RespondAsync(
                    text,
                    embeds,
                    isTts,
                    true,
                    new AllowedMentions(AllowedMentionTypes.Users) { MentionRepliedUser = pingUser },
                    component,
                    embed,
                    options
                );
                return;
            }

            await command.FollowupAsync(
                text,
                embeds,
                isTts,
                true,
                new AllowedMentions(AllowedMentionTypes.Users) { MentionRepliedUser = pingUser },
                component,
                embed,
                options
            );
        }

        public static async Task RespondEphemeralPingAsync(
            this SocketCommandBase command,
            string text = null,
            Embed[] embeds = null,
            bool isTts = false,
            MessageComponent component = null,
            Embed embed = null,
            RequestOptions options = null
        ) => await command.RespondEphemeralAsync(text, true, embeds, isTts, component, embed, options);
    }

    public static class AdminUtility
    {
        private static IEnumerable<string> AllowedChannelIDs =>
            (from id in global::Grid.Bot.Properties.Settings.Default.AllowedChannels.Split(',')
             where !id.IsNullOrEmpty()
             select id).ToArray();

        private static IEnumerable<string> AdministratorUserIDs =>
            (from id in global::Grid.Bot.Properties.Settings.Default.Admins.Split(',')
             where !id.IsNullOrEmpty()
             select id).ToArray();

        private static IEnumerable<string> HigherPrivilagedUserIDs =>
            (from id in global::Grid.Bot.Properties.Settings.Default.HigherPrivilagedUsers.Split(',')
             where !id.IsNullOrEmpty()
             select id).ToArray();

        private static IEnumerable<string> BlacklistedUserIDs =>
            (from id in global::Grid.Bot.Properties.Settings.Default.BlacklistedDiscordUserIds.Split(',')
             where !id.IsNullOrEmpty()
             select id).ToArray();

        public static bool UserIsOwner(IUser user) => UserIsOwner(user.Id);

        public static bool UserIsOwner(ulong id) => id == global::Grid.Bot.Properties.Settings.Default.BotOwnerID;

        public static bool UserIsAdmin(IUser user) => UserIsAdmin(user.Id);

        public static bool UserIsAdmin(ulong id) => UserIsAdmin(id.ToString());

        public static bool UserIsAdmin(string id) => UserIsOwner(ulong.Parse(id)) || AdministratorUserIDs.Contains(id);

        public static bool UserIsPrivilaged(IUser user) => UserIsPrivilaged(user.Id);

        public static bool UserIsPrivilaged(ulong id) => UserIsPrivilaged(id.ToString());

        public static bool UserIsPrivilaged(string id) => UserIsAdmin(id) || HigherPrivilagedUserIDs.Contains(id);

        public static bool UserIsBlacklisted(IUser user) => UserIsBlacklisted(user.Id);

        public static bool UserIsBlacklisted(ulong id) => UserIsBlacklisted(id.ToString());

        public static bool UserIsBlacklisted(string id) => !UserIsOwner(ulong.Parse(id)) && BlacklistedUserIDs.Contains(id);

        public static bool ChannelIsAllowed(IChannel channel) => ChannelIsAllowed(channel.Id);

        public static bool ChannelIsAllowed(ulong id) => ChannelIsAllowed(id.ToString());

        public static bool ChannelIsAllowed(string id) => AllowedChannelIDs.Contains(id);

        public static bool ChannelIsAllowed(this SocketMessage message)
        {
            var allowedChannelIds = message.GetSetting<string>("AllowedChannels").Split(',');
            return allowedChannelIds.Contains(message.Channel.Id.ToString());
        }

#if WE_LOVE_EM_SLASH_COMMANDS
        public static async Task<bool> RejectIfNotPrivilagedAsync(SocketCommandBase command)
        {
            var isPrivilaged = UserIsPrivilaged(command.User);

            if (!isPrivilaged)
            {
                await command.RespondEphemeralPingAsync("Only privilaged users or administrators can execute that command.");
                return false;
            }

            Logger.Singleton.Information("User '{0}' is privilaged or an admin.", command.User.Id);
            return true;
        }

        public static async Task<bool> RejectIfNotAdminAsync(SocketCommandBase command)
        {
            var isAdmin = UserIsAdmin(command.User);

            if (!isAdmin)
            {
                await command.RespondEphemeralPingAsync("You lack the correct permissions to execute that command.");
                return false;
            }

            Logger.Singleton.Information("User '{0}' is an admin.", command.User.Id);
            return true;
        }
        public static async Task<bool> RejectIfNotOwnerAsync(SocketCommandBase command)
        {
            var isOwner = UserIsOwner(command.User);

            if (!isOwner)
            {
                await command.RespondEphemeralPingAsync("You lack the correct permissions to execute that command.");
                return false;
            }

            Logger.Singleton.Information("User '{0}' is the owner.", command.User.Id);
            return true;
        }
#endif

        public static async Task<bool> RejectIfNotPrivilagedAsync(SocketMessage message)
        {
            var isPrivilaged = UserIsPrivilaged(message.Author);

            if (!isPrivilaged)
            {
                await message.ReplyAsync("Only privilaged users or administrators can execute that command.");
                return false;
            }

            Logger.Singleton.Information("User '{0}' is privilaged or an admin.", message.Author.Id);
            return true;
        }

        public static async Task<bool> RejectIfNotAdminAsync(SocketMessage message)
        {
            if (!UserIsAdmin(message.Author))
            {
                Logger.Singleton.Warning("User '{0}' is not on the admin whitelist. Please take this with " +
                                               "caution as leaked internal methods may be abused!", message.Author.Id);
                await message.ReplyAsync("You lack the correct permissions to execute that command.");
                return false;
            }
            Logger.Singleton.Information("User '{0}' is on the admin whitelist.", message.Author.Id);
            return true;
        }

        public static async Task<bool> RejectIfNotOwnerAsync(SocketMessage message)
        {
            if (!UserIsOwner(message.Author))
            {
                Logger.Singleton.Warning("User '{0}' is not the owner. Please take this with caution " +
                                               "as leaked internal methods may be abused!", message.Author.Id);
                await message.ReplyAsync("You lack the correct permissions to execute that command.");
                return false;
            }
            Logger.Singleton.Information("User '{0}' is the owner.", message.Author.Id);
            return true;
        }
    }
}
