namespace Grid.Bot.Extensions;

using System.IO;
using System.Threading.Tasks;

using Discord;

/// <summary>
/// Extension methods for <see cref="IUserMessage"/>
/// </summary>
public static class IUserMessageExtensions
{
    /// <summary>
    ///     Sends an inline reply that references a message.
    /// </summary>
    /// <param name="msg">The message that is being replied on.</param>
    /// <param name="fileStream">The stream of the file to send.</param>
    /// <param name="fileName">The name of the file to send.</param>
    /// <param name="text">The message to be sent.</param>
    /// <param name="isTTS">Determines whether the message should be read aloud by Discord or not.</param>
    /// <param name="embed">The <see cref="Discord.EmbedType.Rich"/> <see cref="Embed"/> to be sent.</param>
    /// <param name="embeds">A array of <see cref="Embed"/>s to send with this response. Max 10.</param>
    /// <param name="allowedMentions">
    ///     Specifies if notifications are sent for mentioned users and roles in the message <paramref name="text"/>.
    ///     If <see langword="null" />, all mentioned roles and users will be notified.
    /// </param>
    /// <param name="options">The options to be used when sending the request.</param>
    /// <param name="isSpoiler">Determines whether the file should be sent as a spoiler or not.</param>
    /// <param name="components">The message components to be included with this message. Used for interactions.</param>
    /// <param name="stickers">A collection of stickers to send with the message.</param>
    /// <param name="flags">Message flags combined as a bitfield.</param>
    /// <returns>
    ///     A task that represents an asynchronous send operation for delivering the message. The task result
    ///     contains the sent message.
    /// </returns>
    public static Task<IUserMessage> ReplyWithFileAsync(
        this IUserMessage msg, 
        Stream fileStream, 
        string fileName, 
        string text = null, 
        bool isTTS = false, 
        Embed embed = null, 
        AllowedMentions allowedMentions = null, 
        RequestOptions options = null, 
        bool isSpoiler = false, 
        MessageComponent components = null, 
        ISticker[] stickers = null, 
        Embed[] embeds = null, 
        MessageFlags flags = MessageFlags.None
    )
        => msg.Channel.SendFileAsync(
            fileStream, 
            fileName,
            text, 
            isTTS, 
            embed, 
            options, 
            isSpoiler,
            allowedMentions, 
            new MessageReference(messageId: msg.Id), 
            components, 
            stickers, 
            embeds, 
            flags
        );
}