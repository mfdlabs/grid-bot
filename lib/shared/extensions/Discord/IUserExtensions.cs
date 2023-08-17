using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Diagnostics;
using Grid.Bot.Global;
using Grid.Bot.Utility;
using Text.Extensions;
using Threading.Extensions;

namespace Grid.Bot.Extensions
{
    public static class IUserExtensions
    {
        public static void Whitelist(this IUser user)
        {
            var blacklistedUsers = global::Grid.Bot.Properties.Settings.Default.BlacklistedDiscordUserIds;

            if (blacklistedUsers.Contains(user.Id.ToString()))
            {
                var blIds = blacklistedUsers.Split(',').ToList();
                blIds.Remove(user.Id.ToString());
                global::Grid.Bot.Properties.Settings.Default["BlacklistedDiscordUserIds"] = blIds.Join(',');
                global::Grid.Bot.Properties.Settings.Default.Save();
            }
        }
        public static void Blacklist(this IUser user)
        {
            var blacklistedUsers = global::Grid.Bot.Properties.Settings.Default.BlacklistedDiscordUserIds;

            if (!blacklistedUsers.Contains(user.Id.ToString()))
            {
                var blIds = blacklistedUsers.Split(',').ToList();
                blIds.Add(user.Id.ToString());
                global::Grid.Bot.Properties.Settings.Default["BlacklistedDiscordUserIds"] = blIds.Join(',');
                global::Grid.Bot.Properties.Settings.Default.Save();
            }
        }

        public static void Disentitle(this IUser user)
        {
            var privilagedUsers = global::Grid.Bot.Properties.Settings.Default.HigherPrivilagedUsers;

            if (privilagedUsers.Contains(user.Id.ToString()))
            {
                var pIds = privilagedUsers.Split(',').ToList();
                pIds.Remove(user.Id.ToString());
                global::Grid.Bot.Properties.Settings.Default["HigherPrivilagedUsers"] = pIds.Join(',');
                global::Grid.Bot.Properties.Settings.Default.Save();
            }
        }
        public static void Entitle(this IUser user)
        {
            var privilagedUsers = global::Grid.Bot.Properties.Settings.Default.HigherPrivilagedUsers;

            if (!privilagedUsers.Contains(user.Id.ToString()))
            {
                var pIds = privilagedUsers.Split(',').ToList();
                pIds.Add(user.Id.ToString());
                global::Grid.Bot.Properties.Settings.Default["HigherPrivilagedUsers"] = pIds.Join(',');
                global::Grid.Bot.Properties.Settings.Default.Save();
            }
        }

        public static void Demote(this IUser user)
        {
            var admins = global::Grid.Bot.Properties.Settings.Default.Admins;

            if (admins.Contains(user.Id.ToString()))
            {
                var adIds = admins.Split(',').ToList();
                adIds.Remove(user.Id.ToString());
                global::Grid.Bot.Properties.Settings.Default["Admins"] = adIds.Join(',');
                global::Grid.Bot.Properties.Settings.Default.Save();
            }
        }
        public static void Promote(this IUser user)
        {
            var admins = global::Grid.Bot.Properties.Settings.Default.Admins;

            if (!admins.Contains(user.Id.ToString()))
            {
                var adIds = admins.Split(',').ToList();
                adIds.Add(user.Id.ToString());
                global::Grid.Bot.Properties.Settings.Default["Admins"] = adIds.Join(',');
                global::Grid.Bot.Properties.Settings.Default.Save();
            }
        }
        public static bool CanExecuteByRolloutPercentage(this IUser user, int rolloutPercentage)
        {
            if (user.IsAdmin()) return true;

            return PercentageInvoker.CanInvoke(rolloutPercentage);
        }
        public static bool IsBlacklisted(this IUser user) => AdminUtility.UserIsBlacklisted(user);
        public static bool IsAdmin(this IUser user) => AdminUtility.UserIsAdmin(user);
        public static bool IsPrivilaged(this IUser user) => AdminUtility.UserIsPrivilaged(user);
        public static bool IsOwner(this IUser user) => AdminUtility.UserIsOwner(user);
        public static async Task<IUserMessage> SendDirectMessageAsync(
            this IUser user,
            string text = null,
            bool isTts = false,
            Embed embed = null,
            RequestOptions options = null,
            MessageReference messageReference = null
        )
        {
            var dmChannel = await BotRegistry.Client.GetDMChannelAsync(user.Id);
            if (dmChannel == null) dmChannel = await user.CreateDMChannelAsync();
            return await dmChannel?.SendMessageAsync(
                text,
                isTts,
                embed,
                options,
                new AllowedMentions(AllowedMentionTypes.Users) { MentionRepliedUser = true },
                messageReference
            );
        }
        public static async Task<IUserMessage> SendDirectMessageWithFileAsync(
            this IUser user,
            string fileName,
            string text = null,
            bool isTts = false,
            Embed embed = null,
            RequestOptions options = null,
            bool isSpoiler = false,
            MessageReference messageReference = null
        )
        {
            var dmChannel = await BotRegistry.Client.GetDMChannelAsync(user.Id);
            if (dmChannel == null) dmChannel = await user.CreateDMChannelAsync();
            return await dmChannel?.SendFileAsync(
                fileName,
                text,
                isTts,
                embed,
                options,
                isSpoiler,
                new AllowedMentions(AllowedMentionTypes.Users) { MentionRepliedUser = true },
                messageReference
            );
        }
        public static async Task<IUserMessage> SendDirectMessageWithFileAsync(
            this IUser user,
            Stream stream,
            string fileName,
            string text = null,
            bool isTts = false,
            Embed embed = null,
            RequestOptions options = null,
            bool isSpoiler = false,
            MessageReference messageReference = null
        )
        {
            var dmChannel = await BotRegistry.Client.GetDMChannelAsync(user.Id);
            if (dmChannel == null) dmChannel = await user.CreateDMChannelAsync();
            return await dmChannel?.SendFileAsync(
                stream,
                fileName,
                text,
                isTts,
                embed,
                options,
                isSpoiler,
                new AllowedMentions(AllowedMentionTypes.Users) { MentionRepliedUser = true },
                messageReference
            );
        }
        public static IUserMessage SendDirectMessageWithFile(
            this IUser user,
            string fileName,
            string text = null,
            bool isTts = false,
            Embed embed = null,
            RequestOptions options = null,
            bool isSpoiler = false,
            MessageReference messageReference = null
        )
            => user.SendDirectMessageWithFileAsync(fileName, text, isTts, embed, options, isSpoiler, messageReference).Sync();
        public static IUserMessage SendDirectMessageWithFile(
            this IUser user,
            Stream stream,
            string fileName,
            string text = null,
            bool isTts = false,
            Embed embed = null,
            RequestOptions options = null,
            bool isSpoiler = false,
            MessageReference messageReference = null
        )
            => user.SendDirectMessageWithFileAsync(stream, fileName, text, isTts, embed, options, isSpoiler, messageReference).Sync();
        public static IUserMessage SendDirectMessage(
            this IUser user,
            string text = null,
            bool isTts = false,
            Embed embed = null,
            RequestOptions options = null,
            MessageReference messageReference = null
        )
            => user.SendDirectMessageAsync(text, isTts, embed, options, messageReference).Sync();
    }
}
