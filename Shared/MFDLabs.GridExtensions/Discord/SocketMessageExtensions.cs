/* Copyright MFDLABS Corporation. All rights reserved. */

using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Utility;
using MFDLabs.Text.Extensions;
using MFDLabs.Threading.Extensions;

namespace MFDLabs.Grid.Bot.Extensions
{
    public static class SocketMessageExtensions
    {
        public static async Task<RestUserMessage> ReplyAsync(
            this SocketMessage message,
            string text = null,
            bool isTts = false,
            Embed embed = null,
            RequestOptions options = null
        )
        => await message.Channel.SendMessageAsync(
                text,
                isTts,
                embed,
                options,
                new AllowedMentions(AllowedMentionTypes.Users) { MentionRepliedUser = true },
                new MessageReference(message.Id)
            );
        public static async Task<RestUserMessage> ReplyWithFileAsync(
            this SocketMessage message,
            string fileName,
            string text = null,
            bool isTts = false,
            Embed embed = null,
            RequestOptions options = null,
            bool isSpoiler = false
        )
            => await message.Channel.SendFileAsync(
                fileName,
                text,
                isTts,
                embed,
                options,
                isSpoiler,
                new AllowedMentions(AllowedMentionTypes.Users) { MentionRepliedUser = true },
                new MessageReference(message.Id)
            );
        public static async Task<RestUserMessage> ReplyWithFileAsync(
            this SocketMessage message,
            Stream stream,
            string fileName,
            string text = null,
            bool isTts = false,
            Embed embed = null,
            RequestOptions options = null,
            bool isSpoiler = false
        )
            => await message.Channel.SendFileAsync(
                stream,
                fileName,
                text,
                isTts,
                embed,
                options,
                isSpoiler,
                new AllowedMentions(AllowedMentionTypes.Users) { MentionRepliedUser = true },
                new MessageReference(message.Id)
            );
        public static RestUserMessage ReplyWithFile(
            this SocketMessage message,
            string fileName,
            string text = null,
            bool isTts = false,
            Embed embed = null,
            RequestOptions options = null,
            bool isSpoiler = false
        )
            => message.ReplyWithFileAsync(fileName, text, isTts, embed, options, isSpoiler).Sync();
        public static RestUserMessage ReplyWithFile(
            this SocketMessage message,
            Stream stream,
            string fileName,
            string text = null,
            bool isTts = false,
            Embed embed = null,
            RequestOptions options = null,
            bool isSpoiler = false
        )
            => message.ReplyWithFileAsync(stream, fileName, text, isTts, embed, options, isSpoiler).Sync();
        public static RestUserMessage Reply
            (this SocketMessage message,
            string text = null,
            bool isTts = false,
            Embed embed = null,
            RequestOptions options = null
        )
            => message.ReplyAsync(text, isTts, embed, options).Sync();
        public static bool IsInPublicChannel(this SocketMessage message) =>
            message.Channel is SocketGuildChannel;
        public static async Task<bool> RejectIfNotAdminAsync(this SocketMessage message) 
            => await AdminUtility.RejectIfNotAdminAsync(message);
        public static async Task<bool> RejectIfNotPrivilagedAsync(this SocketMessage message)
            => await AdminUtility.RejectIfNotPrivilagedAsync(message);
        public static async Task<bool> RejectIfNotOwnerAsync(this SocketMessage message)
            => await AdminUtility.RejectIfNotOwnerAsync(message);
        public static bool RejectIfNotPrivilaged(this SocketMessage message) 
            => message.RejectIfNotPrivilagedAsync().Sync();
        public static bool RejectIfNotAdmin(this SocketMessage message)
            => message.RejectIfNotAdminAsync().Sync();
        public static bool RejectIfNotOwner(this SocketMessage message) 
            => message.RejectIfNotOwnerAsync().Sync();
    }
}
