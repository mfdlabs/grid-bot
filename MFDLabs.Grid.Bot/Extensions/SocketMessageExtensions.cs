using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Utility;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Grid.Bot.Extensions
{
    public static class SocketMessageExtensions
    {
        public static async Task<RestUserMessage> ReplyAsync(this SocketMessage message,
            string text = null,
            bool isTts = false,
            Embed embed = null,
            RequestOptions options = null,
            MessageReference messageReference = null)
            => await message.Channel.SendMessageAsync($"<@{message.Author.Id}>{(!text.IsNullOrEmpty() ? ", " : "")}{text}", isTts, embed, options, new AllowedMentions(AllowedMentionTypes.Users), messageReference);
        public static async Task<RestUserMessage> ReplyWithFileAsync(this SocketMessage message,
            string fileName,
            string text = null,
            bool isTts = false,
            Embed embed = null,
            RequestOptions options = null,
            bool isSpoiler = false,
            MessageReference messageReference = null)
            => await message.Channel.SendFileAsync(fileName, $"<@{message.Author.Id}>{(!text.IsNullOrEmpty() ? ", " : "")}{text}", isTts, embed, options, isSpoiler, new AllowedMentions(AllowedMentionTypes.Users), messageReference);
        public static async Task<RestUserMessage> ReplyWithFileAsync(this SocketMessage message,
            Stream stream,
            string fileName,
            string text = null,
            bool isTts = false,
            Embed embed = null,
            RequestOptions options = null,
            bool isSpoiler = false,
            MessageReference messageReference = null)
            => await message.Channel.SendFileAsync(stream, fileName, $"<@{message.Author.Id}>{(!text.IsNullOrEmpty() ? ", " : "")}{text}", isTts, embed, options, isSpoiler, new AllowedMentions(AllowedMentionTypes.Users), messageReference);
        public static RestUserMessage ReplyWithFile(this SocketMessage message,
            string fileName,
            string text = null,
            bool isTts = false,
            Embed embed = null,
            RequestOptions options = null,
            bool isSpoiler = false,
            MessageReference messageReference = null)
            => message.ReplyWithFileAsync(fileName, text, isTts, embed, options, isSpoiler, messageReference).GetAwaiter().GetResult();
        public static RestUserMessage ReplyWithFile(this SocketMessage message,
            Stream stream,
            string fileName,
            string text = null,
            bool isTts = false,
            Embed embed = null,
            RequestOptions options = null,
            bool isSpoiler = false,
            MessageReference messageReference = null)
            => message.ReplyWithFileAsync(stream, fileName, text, isTts, embed, options, isSpoiler, messageReference).GetAwaiter().GetResult();
        public static RestUserMessage Reply(this SocketMessage message,
            string text = null,
            bool isTts = false,
            Embed embed = null,
            RequestOptions options = null,
            MessageReference messageReference = null)
            => message.ReplyAsync(text, isTts, embed, options, messageReference).GetAwaiter().GetResult();
        public static bool IsInPublicChannel(this SocketMessage message) =>
            message.Channel is SocketGuildChannel;
        public static async Task<bool> RejectIfNotAdminAsync(this SocketMessage message) 
            => await AdminUtility.RejectIfNotAdminAsync(message);
        public static async Task<bool> RejectIfNotPrivilagedAsync(this SocketMessage message)
            => await AdminUtility.RejectIfNotPrivilagedAsync(message);
        public static async Task<bool> RejectIfNotOwnerAsync(this SocketMessage message)
            => await AdminUtility.RejectIfNotOwnerAsync(message);
        public static bool RejectIfNotPrivilaged(this SocketMessage message) 
            => message.RejectIfNotPrivilagedAsync().GetAwaiter().GetResult();
        public static bool RejectIfNotAdmin(this SocketMessage message)
            => message.RejectIfNotAdminAsync().GetAwaiter().GetResult();
        public static bool RejectIfNotOwner(this SocketMessage message) 
            => message.RejectIfNotOwnerAsync().GetAwaiter().GetResult();
    }
}
