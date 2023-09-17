namespace Grid.Bot.Extensions;

using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.Rest;
using Discord.WebSocket;

using Utility;

/// <summary>
/// Extension methods for <see cref="SocketMessage"/>
/// </summary>
public static class SocketMessageExtensions
{
    /// <summary>
    /// Replies to the specified <see cref="SocketMessage"/>
    /// </summary>
    /// <param name="message">The <see cref="SocketMessage"/></param>
    /// <param name="text">The message text.</param>
    /// <param name="isTts">Is the message a TTS message?</param>
    /// <param name="embed">The <see cref="Embed"/></param>
    /// <param name="options">The <see cref="RequestOptions"/></param>
    /// <returns>A <see cref="RestUserMessage"/></returns>
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

    /// <summary>
    /// Replies to the specified <see cref="SocketMessage"/> with a file.
    /// </summary>
    /// <param name="message">The <see cref="SocketMessage"/></param>
    /// <param name="stream">The <see cref="Stream"/>.</param>
    /// <param name="fileName">The name of the file..</param>
    /// <param name="text">The message text.</param>
    /// <param name="isTts">Is the message a TTS message?</param>
    /// <param name="embed">The <see cref="Embed"/></param>
    /// <param name="options">The <see cref="RequestOptions"/></param>
    /// <param name="isSpoiler">Is the file a spoiler?</param>
    /// <returns>A <see cref="RestUserMessage"/></returns>
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

    /// <summary>
    /// Replies to the specified <see cref="SocketMessage"/> with files.
    /// </summary>
    /// <param name="message">The <see cref="SocketMessage"/></param>
    /// <param name="attachments">The <see cref="ICollection{T}"/> of <see cref="FileAttachment"/>s.</param>
    /// <param name="text">The message text.</param>
    /// <param name="isTts">Is the message a TTS message?</param>
    /// <param name="embed">The <see cref="Embed"/></param>
    /// <param name="options">The <see cref="RequestOptions"/></param>
    /// <returns>A <see cref="RestUserMessage"/></returns>
    public static async Task<RestUserMessage> ReplyWithFilesAsync(
        this SocketMessage message,
        IEnumerable<FileAttachment> attachments,
        string text = null,
        bool isTts = false,
        Embed embed = null,
        RequestOptions options = null
    )
        => await message.Channel.SendFilesAsync(
            attachments,
            text,
            isTts,
            embed,
            options,
            new AllowedMentions(AllowedMentionTypes.Users) { MentionRepliedUser = true },
            new MessageReference(message.Id)
        );
    
    /// <summary>
    /// Reject if the <see cref="IUser"/> is not an Admin.
    /// </summary>
    /// <param name="message">The <see cref="SocketMessage"/></param>
    /// <returns>True if the user has access</returns>
    public static async Task<bool> RejectIfNotAdminAsync(this SocketMessage message) 
        => await AdminUtility.RejectIfNotAdminAsync(message);

    /// <summary>
    /// Reject if the <see cref="IUser"/> is not the owner.
    /// </summary>
    /// <param name="message">The <see cref="SocketMessage"/></param>
    /// <returns>True if the user has access</returns>
    public static async Task<bool> RejectIfNotOwnerAsync(this SocketMessage message)
        => await AdminUtility.RejectIfNotOwnerAsync(message);
}
