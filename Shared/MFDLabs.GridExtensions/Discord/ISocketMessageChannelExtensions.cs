/* Copyright MFDLABS Corporation. All rights reserved. */

using System.Collections.Generic;
using System.IO;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Utility;
using MFDLabs.Threading.Extensions;

namespace MFDLabs.Grid.Bot.Extensions
{
    public static class SocketMessageChannelExtensions
    {
        public static RestUserMessage SendMessage(this ISocketMessageChannel channel,
            string text = null,
            bool isTts = false,
            Embed embed = null,
            RequestOptions options = null,
            AllowedMentions allowedMentions = null,
            MessageReference messageReference = null) 
            => channel.SendMessageAsync(text, isTts, embed, options, allowedMentions, messageReference).Sync();
        public static RestUserMessage SendFile(this ISocketMessageChannel channel,
            string filePath,
            string text = null,
            bool isTts = false,
            Embed embed = null,
            RequestOptions options = null,
            bool isSpoiler = false,
            AllowedMentions allowedMentions = null,
            MessageReference messageReference = null) 
            => channel.SendFileAsync(filePath, text, isTts, embed, options, isSpoiler, allowedMentions, messageReference).Sync();
        public static RestUserMessage SendFile(this ISocketMessageChannel channel,
            Stream stream,
            string filename,
            string text = null,
            bool isTts = false,
            Embed embed = null,
            RequestOptions options = null,
            bool isSpoiler = false,
            AllowedMentions allowedMentions = null,
            MessageReference messageReference = null) 
            => channel.SendFileAsync(stream, filename, text, isTts, embed, options, isSpoiler, allowedMentions, messageReference).Sync();
        public static IReadOnlyCollection<RestMessage> GetPinnedMessages(this ISocketMessageChannel channel, RequestOptions options = null)
            => channel.GetPinnedMessagesAsync(options).Sync();
        public static bool IsWhitelisted(this ISocketMessageChannel channel) => AdminUtility.ChannelIsAllowed(channel);
    }
}
