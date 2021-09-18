using Discord;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Utility;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Extensions
{
    public static class SocketMessageExtensions
    {
        public static async Task ReplyAsync(this SocketMessage message, string text = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, MessageReference messageReference = null)
        {
            await message.Channel.SendMessageAsync($"<@{message.Author.Id}>, {text}", isTTS, embed, options, new AllowedMentions(AllowedMentionTypes.Users), messageReference);
        }

        public static void Reply(this SocketMessage message, string text = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, MessageReference messageReference = null)
        {
            message.ReplyAsync(text, isTTS, embed, options, messageReference).GetAwaiter().GetResult();
        }

        public static bool IsInPublicChannel(this SocketMessage message)
        {
            return message.Channel as SocketGuildChannel != null;
        }

        public static async Task<bool> RejectIfNotAdminAsync(this SocketMessage message)
        {
            return await AdminUtility.Singleton.RejectIfNotAdminAsync(message);
        }

        public static async Task<bool> RejectIfNotPrivilagedAsync(this SocketMessage message)
        {
            return await AdminUtility.Singleton.RejectIfNotPrivilagedAsync(message);
        }

        public static bool RejectIfNotPrivilaged(this SocketMessage message)
        {
            return message.RejectIfNotPrivilagedAsync().GetAwaiter().GetResult();
        }

        public static bool RejectIfNotAdmin(this SocketMessage message)
        {
            return message.RejectIfNotAdminAsync().GetAwaiter().GetResult();
        }
    }
}
