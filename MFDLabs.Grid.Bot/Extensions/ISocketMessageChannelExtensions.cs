using System.Collections.Generic;
using System.IO;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Utility;

namespace MFDLabs.Grid.Bot.Extensions
{
    internal static class ISocketMessageChannelExtensions
    {
        public static RestUserMessage SendMessage(this ISocketMessageChannel channel, string text = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, AllowedMentions allowedMentions = null, MessageReference messageReference = null) 
            => channel.SendMessageAsync(text, isTTS, embed, options, allowedMentions, messageReference).GetAwaiter().GetResult();
        public static RestUserMessage SendFile(this ISocketMessageChannel channel, string filePath, string text = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, bool isSpoiler = false, AllowedMentions allowedMentions = null, MessageReference messageReference = null) 
            => channel.SendFileAsync(filePath, text, isTTS, embed, options, isSpoiler, allowedMentions, messageReference).GetAwaiter().GetResult();
        public static RestUserMessage SendFile(this ISocketMessageChannel channel, Stream stream, string filename, string text = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, bool isSpoiler = false, AllowedMentions allowedMentions = null, MessageReference messageReference = null) 
            => channel.SendFileAsync(stream, filename, text, isTTS, embed, options, isSpoiler, allowedMentions, messageReference).GetAwaiter().GetResult();
        public static IReadOnlyCollection<RestMessage> GetPinnedMessages(this ISocketMessageChannel channel, RequestOptions options = null)
            => channel.GetPinnedMessagesAsync(options).GetAwaiter().GetResult();
        public static bool IsWhitelisted(this ISocketMessageChannel channel) => AdminUtility.Singleton.ChannelIsAllowed(channel);
    }
}
