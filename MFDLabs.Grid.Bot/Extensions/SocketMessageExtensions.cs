﻿using Discord;
using Discord.Rest;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Utility;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Extensions
{
    public static class SocketMessageExtensions
    {
        public static async Task<RestUserMessage> ReplyAsync(this SocketMessage message, string text = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, MessageReference messageReference = null)
            => await message.Channel.SendMessageAsync($"<@{message.Author.Id}>, {text}", isTTS, embed, options, new AllowedMentions(AllowedMentionTypes.Users), messageReference);
        public static void Reply(this SocketMessage message, string text = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, MessageReference messageReference = null)
            => message.ReplyAsync(text, isTTS, embed, options, messageReference).GetAwaiter().GetResult();
        public static bool IsInPublicChannel(this SocketMessage message) => message.Channel as SocketGuildChannel != null;
        public static async Task<bool> RejectIfNotAdminAsync(this SocketMessage message) => await AdminUtility.Singleton.RejectIfNotAdminAsync(message);
        public static async Task<bool> RejectIfNotPrivilagedAsync(this SocketMessage message) => await AdminUtility.Singleton.RejectIfNotPrivilagedAsync(message);
        public static async Task<bool> RejectIfNotOwnerAsync(this SocketMessage message) => await AdminUtility.Singleton.RejectIfNotOwnerAsync(message);
        public static bool RejectIfNotPrivilaged(this SocketMessage message) => message.RejectIfNotPrivilagedAsync().GetAwaiter().GetResult();
        public static bool RejectIfNotAdmin(this SocketMessage message) => message.RejectIfNotAdminAsync().GetAwaiter().GetResult();
        public static bool RejectIfNotOwner(this SocketMessage message) => message.RejectIfNotOwnerAsync().GetAwaiter().GetResult();
    }
}
