using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System.Collections.Generic;
using System.IO;

namespace MFDLabs.Grid.Bot.Extensions
{
    internal static class ISocketMessageChannelExtensions
    {
        public static RestUserMessage SendMessage(this ISocketMessageChannel channel, string text = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, AllowedMentions allowedMentions = null, MessageReference messageReference = null)
        {
            return channel.SendMessageAsync(text, isTTS, embed, options, allowedMentions, messageReference).GetAwaiter().GetResult();
        }

        public static RestUserMessage SendFile(this ISocketMessageChannel channel, string filePath, string text = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, bool isSpoiler = false, AllowedMentions allowedMentions = null, MessageReference messageReference = null)
        {
            return channel.SendFileAsync(filePath, text, isTTS, embed, options, isSpoiler, allowedMentions, messageReference).GetAwaiter().GetResult();
        }

        public static RestUserMessage SendFile(this ISocketMessageChannel channel, Stream stream, string filename, string text = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, bool isSpoiler = false, AllowedMentions allowedMentions = null, MessageReference messageReference = null)
        {
            return channel.SendFileAsync(stream, filename, text, isTTS, embed, options, isSpoiler, allowedMentions, messageReference).GetAwaiter().GetResult();
        }

        public static IReadOnlyCollection<RestMessage> GetPinnedMessages(this ISocketMessageChannel channel, RequestOptions options = null)
        {
            return channel.GetPinnedMessagesAsync(options).GetAwaiter().GetResult();
        }
    }
}
