﻿using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using MFDLabs.Analytics.Google;
using MFDLabs.Grid.Bot.Global;
using MFDLabs.Grid.Bot.Utility;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Grid.Bot.Extensions
{
    public static class IUserExtensions
    {
        public static bool IsBlacklisted(this IUser user) => AdminUtility.UserIsBlacklisted(user);
        public static bool IsAdmin(this IUser user) => AdminUtility.UserIsAdmin(user);
        public static bool IsPrivilaged(this IUser user) => AdminUtility.UserIsPrivilaged(user);
        public static bool IsOwner(this IUser user) => AdminUtility.UserIsOwner(user);
        public static Task<long?> GetRobloxIdAsync(this IUser user) => UserUtility.GetRobloxIdByIUserAsync(user);
        public static long? GetRobloxId(this IUser user) => UserUtility.GetRobloxIdByIUser(user);
        public static void PageViewed(this IUser user, string location)
            => GoogleAnalyticsManager.TrackPageView(user.Id.ToString(), location);
        public static Task PageViewedAsync(this IUser user, string location)
            => GoogleAnalyticsManager.TrackPageViewAsync(user.Id.ToString(), location);
        public static void FireEvent(this IUser user, string @event, string extraContext = "none") 
            => GoogleAnalyticsManager.TrackEvent(user.Id.ToString(), "UserAction", @event, extraContext);
        public static Task FireEventAsync(this IUser user, string @event, string extraContext = "none") 
            => GoogleAnalyticsManager.TrackEventAsync(user.Id.ToString(), "UserAction", @event, extraContext);
        public static async Task<IUserMessage> SendDirectMessageAsync(
            this IUser user,
            string text = null,
            bool isTts = false,
            Embed embed = null,
            RequestOptions options = null,
            MessageReference messageReference = null
        )
        {
            var dmChannel = await BotGlobal.Client.GetDMChannelAsync(user.Id);
            if (dmChannel == null) dmChannel = await user.CreateDMChannelAsync();
            return await dmChannel?.SendMessageAsync(
                $"<@{user.Id}>{(!text.IsNullOrEmpty() ? ", " : "")}{text}",
                isTts,
                embed,
                options,
                new AllowedMentions(AllowedMentionTypes.Users),
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
            var dmChannel = await BotGlobal.Client.GetDMChannelAsync(user.Id);
            if (dmChannel == null) dmChannel = await user.CreateDMChannelAsync();
            return await dmChannel?.SendFileAsync(
                fileName,
                $"<@{user.Id}>{(!text.IsNullOrEmpty() ? ", " : "")}{text}",
                isTts,
                embed,
                options,
                isSpoiler,
                new AllowedMentions(AllowedMentionTypes.Users),
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
            var dmChannel = await BotGlobal.Client.GetDMChannelAsync(user.Id);
            if (dmChannel == null) dmChannel = await user.CreateDMChannelAsync();
            return await dmChannel?.SendFileAsync(
                stream,
                fileName,
                $"<@{user.Id}>{(!text.IsNullOrEmpty() ? ", " : "")}{text}",
                isTts,
                embed,
                options,
                isSpoiler,
                new AllowedMentions(AllowedMentionTypes.Users),
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
            => user.SendDirectMessageWithFileAsync(fileName, text, isTts, embed, options, isSpoiler, messageReference).GetAwaiter().GetResult();
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
            => user.SendDirectMessageWithFileAsync(stream, fileName, text, isTts, embed, options, isSpoiler, messageReference).GetAwaiter().GetResult();
        public static IUserMessage SendDirectMessage(
            this IUser user,
            string text = null,
            bool isTts = false,
            Embed embed = null,
            RequestOptions options = null,
            MessageReference messageReference = null
        )
            => user.SendDirectMessageAsync(text, isTts, embed, options, messageReference).GetAwaiter().GetResult();
    }
}