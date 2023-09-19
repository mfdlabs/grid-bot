#if WE_LOVE_EM_SLASH_COMMANDS

namespace Grid.Bot.Extensions;

using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.WebSocket;

using Utility;

/// <summary>
/// Extension methods for <see cref="SocketCommandBase"/>
/// </summary>
public static class SocketCommandBaseExtensions
{
    private class DiscardDisposable : IDisposable
    {
        public static DiscardDisposable Instance = new();

        public void Dispose() { }
    }

    /// <summary>
    /// Respond to the specified <see cref="SocketCommandBase"/> or
    /// follow up if it has already been acknowledged.
    /// </summary>
    /// <remarks>This will make it so that everyone can see the message.</remarks>
    /// <param name="command">The <see cref="SocketCommandBase"/></param>
    /// <param name="text">The text.</param>
    /// <param name="pingUser">Should the user be pinged?</param>
    /// <param name="embeds">The <see cref="Embed"/>s</param>
    /// <param name="isTts">Is the mssage a TTS message?</param>
    /// <param name="component">The <see cref="MessageComponent"/></param>
    /// <param name="embed">The <see cref="Embed"/></param>
    /// <param name="options">The <see cref="RequestOptions"/></param>
    public static async Task RespondPublicAsync(
        this SocketCommandBase command,
        string text = null,
        bool pingUser = false,
        Embed[] embeds = null,
        bool isTts = false,
        MessageComponent component = null,
        Embed embed = null,
        RequestOptions options = null
    )
    {
        if (!command.HasResponded)
        {
            await command.RespondAsync(
                text,
                embeds,
                isTts,
                false,
                new AllowedMentions(AllowedMentionTypes.Users) { MentionRepliedUser = pingUser },
                component,
                embed,
                options
            );

            return;
        }

        await command.FollowupAsync(
            text,
            embeds,
            isTts,
            false,
            new AllowedMentions(AllowedMentionTypes.Users) { MentionRepliedUser = pingUser },
            component,
            embed,
            options
        );
    }

    /// <summary>
    /// Respond to the specified <see cref="SocketCommandBase"/> or
    /// follow up if it has already been acknowledged.
    /// </summary>
    /// <remarks>This will make it so that everyone can see the message.</remarks>
    /// <param name="command">The <see cref="SocketCommandBase"/></param>
    /// <param name="text">The text.</param>
    /// <param name="embeds">The <see cref="Embed"/>s</param>
    /// <param name="isTts">Is the mssage a TTS message?</param>
    /// <param name="component">The <see cref="MessageComponent"/></param>
    /// <param name="embed">The <see cref="Embed"/></param>
    /// <param name="options">The <see cref="RequestOptions"/></param>
    public static async Task RespondPublicPingAsync(
        this SocketCommandBase command,
        string text = null,
        Embed[] embeds = null,
        bool isTts = false,
        MessageComponent component = null,
        Embed embed = null,
        RequestOptions options = null
    ) => await command.RespondPublicAsync(text, true, embeds, isTts, component, embed, options);

    /// <summary>
    /// Respond to the specified <see cref="SocketCommandBase"/> or
    /// follow up if it has already been acknowledged.
    /// </summary>
    /// <remarks>This will make it so that only the message owner can see the message.</remarks>
    /// <param name="command">The <see cref="SocketCommandBase"/></param>
    /// <param name="text">The text.</param>
    /// <param name="pingUser">Should the user be pinged?</param>
    /// <param name="embeds">The <see cref="Embed"/>s</param>
    /// <param name="isTts">Is the mssage a TTS message?</param>
    /// <param name="component">The <see cref="MessageComponent"/></param>
    /// <param name="embed">The <see cref="Embed"/></param>
    /// <param name="options">The <see cref="RequestOptions"/></param>
    public static async Task RespondEphemeralAsync(
        this SocketCommandBase command,
        string text = null,
        bool pingUser = false,
        Embed[] embeds = null,
        bool isTts = false,
        MessageComponent component = null,
        Embed embed = null,
        RequestOptions options = null
    )
    {
        if (!command.HasResponded)
        {
            await command.RespondAsync(
                text,
                embeds,
                isTts,
                true,
                new AllowedMentions(AllowedMentionTypes.Users) { MentionRepliedUser = pingUser },
                component,
                embed,
                options
            );

            return;
        }

        await command.FollowupAsync(
            text,
            embeds,
            isTts,
            true,
            new AllowedMentions(AllowedMentionTypes.Users) { MentionRepliedUser = pingUser },
            component,
            embed,
            options
        );
    }

    /// <summary>
    /// Respond to the specified <see cref="SocketCommandBase"/> or
    /// follow up if it has already been acknowledged.
    /// </summary>
    /// <remarks>This will make it so that only the message owner can see the message.</remarks>
    /// <param name="command">The <see cref="SocketCommandBase"/></param>
    /// <param name="text">The text.</param>
    /// <param name="embeds">The <see cref="Embed"/>s</param>
    /// <param name="isTts">Is the mssage a TTS message?</param>
    /// <param name="component">The <see cref="MessageComponent"/></param>
    /// <param name="embed">The <see cref="Embed"/></param>
    /// <param name="options">The <see cref="RequestOptions"/></param>
    public static async Task RespondEphemeralPingAsync(
        this SocketCommandBase command,
        string text = null,
        Embed[] embeds = null,
        bool isTts = false,
        MessageComponent component = null,
        Embed embed = null,
        RequestOptions options = null
    ) => await command.RespondEphemeralAsync(text, true, embeds, isTts, component, embed, options);

    /// <summary>
    /// Respond to the specified <see cref="SocketCommandBase"/> or
    /// follow up if it has already been acknowledged with a file.
    /// </summary>
    /// <remarks>This will make it so that only the message owner can see the message.</remarks>
    /// <param name="command">The <see cref="SocketCommandBase"/></param>
    /// <param name="fileStream">The <see cref="Stream"/>.</param>
    /// <param name="fileName">The file name.</param>
    /// <param name="text">The text.</param>
    /// <param name="pingUser">Should the user be pinged?</param>
    /// <param name="embeds">The <see cref="Embed"/>s</param>
    /// <param name="isTts">Is the mssage a TTS message?</param>
    /// <param name="components">The <see cref="MessageComponent"/></param>
    /// <param name="embed">The <see cref="Embed"/></param>
    /// <param name="options">The <see cref="RequestOptions"/></param>
    public static async Task RespondWithFileEphemeralAsync(
        this SocketCommandBase command,
        Stream fileStream,
        string fileName,
        string text = null,
        bool pingUser = false,
        Embed[] embeds = null,
        bool isTts = false,
        MessageComponent components = null,
        Embed embed = null,
        RequestOptions options = null
    )
    {
        if (!command.HasResponded)
        {
            await command.RespondWithFileAsync(
                fileStream,
                fileName,
                text,
                embeds,
                isTts,
                true,
                new AllowedMentions(AllowedMentionTypes.Users) { MentionRepliedUser = pingUser },
                components,
                embed,
                options
            );

            return;
        }

        await command.FollowupWithFileAsync(
            fileStream,
            fileName,
            text,
            embeds,
            isTts,
            true,
            new AllowedMentions(AllowedMentionTypes.Users) { MentionRepliedUser = pingUser },
            components,
            embed,
            options
        );
    }

    /// <summary>
    /// Respond to the specified <see cref="SocketCommandBase"/> or
    /// follow up if it has already been acknowledged with a file.
    /// </summary>
    /// <remarks>This will make it so that only the message owner can see the message.</remarks>
    /// <param name="command">The <see cref="SocketCommandBase"/></param>
    /// <param name="fileStream">The <see cref="Stream"/>.</param>
    /// <param name="fileName">The file name.</param>
    /// <param name="text">The text.</param>
    /// <param name="embeds">The <see cref="Embed"/>s</param>
    /// <param name="isTts">Is the mssage a TTS message?</param>
    /// <param name="components">The <see cref="MessageComponent"/></param>
    /// <param name="embed">The <see cref="Embed"/></param>
    /// <param name="options">The <see cref="RequestOptions"/></param>
    public static async Task RespondWithFileEphemeralPingAsync(
        this SocketCommandBase command,
        Stream fileStream,
        string fileName,
        string text = null,
        Embed[] embeds = null,
        bool isTts = false,
        MessageComponent components = null,
        Embed embed = null,
        RequestOptions options = null
    ) => await command.RespondWithFileEphemeralAsync(fileStream, fileName, text, true, embeds, isTts, components, embed, options);

    /// <summary>
    /// Respond to the specified <see cref="SocketCommandBase"/> or
    /// follow up if it has already been acknowledged with a file.
    /// </summary>
    /// <remarks>This will make it so that everyone can see the message.</remarks>
    /// <param name="command">The <see cref="SocketCommandBase"/></param>
    /// <param name="fileStream">The <see cref="Stream"/>.</param>
    /// <param name="fileName">The file name.</param>
    /// <param name="text">The text.</param>
    /// <param name="pingUser">Should the user be pinged?</param>
    /// <param name="embeds">The <see cref="Embed"/>s</param>
    /// <param name="isTts">Is the mssage a TTS message?</param>
    /// <param name="components">The <see cref="MessageComponent"/></param>
    /// <param name="embed">The <see cref="Embed"/></param>
    /// <param name="options">The <see cref="RequestOptions"/></param>
    public static async Task RespondWithFilePublicAsync(
       this SocketCommandBase command,
       Stream fileStream,
       string fileName,
       string text = null,
       bool pingUser = false,
       Embed[] embeds = null,
       bool isTts = false,
       MessageComponent components = null,
       Embed embed = null,
       RequestOptions options = null
    )
    {
        if (!command.HasResponded)
        {
            await command.RespondWithFileAsync(
                fileStream,
                fileName,
                text,
                embeds,
                isTts,
                false,
                new AllowedMentions(AllowedMentionTypes.Users) { MentionRepliedUser = pingUser },
                components,
                embed,
                options
            );

            return;
        }

        await command.FollowupWithFileAsync(
            fileStream,
            fileName,
            text,
            embeds,
            isTts,
            false,
            new AllowedMentions(AllowedMentionTypes.Users) { MentionRepliedUser = pingUser },
            components,
            embed,
            options
        );
    }

    /// <summary>
    /// Respond to the specified <see cref="SocketCommandBase"/> or
    /// follow up if it has already been acknowledged with a file.
    /// </summary>
    /// <remarks>This will make it so that everyone can see the message.</remarks>
    /// <param name="command">The <see cref="SocketCommandBase"/></param>
    /// <param name="fileStream">The <see cref="Stream"/>.</param>
    /// <param name="fileName">The file name.</param>
    /// <param name="text">The text.</param>
    /// <param name="embeds">The <see cref="Embed"/>s</param>
    /// <param name="isTts">Is the mssage a TTS message?</param>
    /// <param name="components">The <see cref="MessageComponent"/></param>
    /// <param name="embed">The <see cref="Embed"/></param>
    /// <param name="options">The <see cref="RequestOptions"/></param>
    public static async Task RespondWithFilePublicPingAsync(
       this SocketCommandBase command,
       Stream fileStream,
       string fileName,
       string text = null,
       Embed[] embeds = null,
       bool isTts = false,
       MessageComponent components = null,
       Embed embed = null,
       RequestOptions options = null
    ) => await command.RespondWithFilePublicAsync(fileStream, fileName, text, true, embeds, isTts, components, embed, options);

    /// <summary>
    /// Respond to the specified <see cref="SocketCommandBase"/> or
    /// follow up if it has already been acknowledged with files.
    /// </summary>
    /// <remarks>This will make it so that everyone can see the message.</remarks>
    /// <param name="command">The <see cref="SocketCommandBase"/></param>
    /// <param name="attachments">The <see cref="ICollection{T}"/> of <see cref="FileAttachment"/>s.</param>
    /// <param name="text">The text.</param>
    /// <param name="pingUser">Should the user be pinged?</param>
    /// <param name="embeds">The <see cref="Embed"/>s</param>
    /// <param name="isTts">Is the mssage a TTS message?</param>
    /// <param name="components">The <see cref="MessageComponent"/></param>
    /// <param name="embed">The <see cref="Embed"/></param>
    /// <param name="options">The <see cref="RequestOptions"/></param>
    public static async Task RespondWithFilesPublicAsync(
       this SocketCommandBase command,
       ICollection<FileAttachment> attachments,
       string text = null,
       bool pingUser = false,
       Embed[] embeds = null,
       bool isTts = false,
       MessageComponent components = null,
       Embed embed = null,
       RequestOptions options = null
    )
    {
        if (!command.HasResponded)
        {
            await command.RespondWithFilesAsync(
                attachments,
                text,
                embeds,
                isTts,
                false,
                new AllowedMentions(AllowedMentionTypes.Users) { MentionRepliedUser = pingUser },
                components,
                embed,
                options
            );

            return;
        }

        await command.FollowupWithFilesAsync(
            attachments,
            text,
            embeds,
            isTts,
            false,
            new AllowedMentions(AllowedMentionTypes.Users) { MentionRepliedUser = pingUser },
            components,
            embed,
            options
        );
    }

    /// <summary>
    /// Respond to the specified <see cref="SocketCommandBase"/> or
    /// follow up if it has already been acknowledged with files.
    /// </summary>
    /// <remarks>This will make it so that everyone can see the message.</remarks>
    /// <param name="command">The <see cref="SocketCommandBase"/></param>
    /// <param name="attachments">The <see cref="ICollection{T}"/> of <see cref="FileAttachment"/>s.</param>
    /// <param name="text">The text.</param>
    /// <param name="pingUser">Should the user be pinged?</param>
    /// <param name="embeds">The <see cref="Embed"/>s</param>
    /// <param name="isTts">Is the mssage a TTS message?</param>
    /// <param name="components">The <see cref="MessageComponent"/></param>
    /// <param name="embed">The <see cref="Embed"/></param>
    /// <param name="options">The <see cref="RequestOptions"/></param>
    public static void RespondWithFilesPublic(
       this SocketCommandBase command,
       ICollection<FileAttachment> attachments,
       string text = null,
       bool pingUser = false,
       Embed[] embeds = null,
       bool isTts = false,
       MessageComponent components = null,
       Embed embed = null,
       RequestOptions options = null
   ) => command.RespondWithFilesPublicAsync(
           attachments,
           text,
           pingUser,
           embeds,
           isTts,
           components,
           embed,
           options
       )
       .Wait();

    /// <summary>
    /// Respond to the specified <see cref="SocketCommandBase"/> or
    /// follow up if it has already been acknowledged.
    /// </summary>
    /// <remarks>This will make it so that only the message owner can see the message.</remarks>
    /// <param name="command">The <see cref="SocketCommandBase"/></param>
    /// <param name="text">The text.</param>
    /// <param name="embeds">The <see cref="Embed"/>s</param>
    /// <param name="isTts">Is the mssage a TTS message?</param>
    /// <param name="component">The <see cref="MessageComponent"/></param>
    /// <param name="embed">The <see cref="Embed"/></param>
    /// <param name="options">The <see cref="RequestOptions"/></param>
    public static void RespondEphemeralPing(
        this SocketCommandBase command,
        string text = null,
        Embed[] embeds = null,
        bool isTts = false,
        MessageComponent component = null,
        Embed embed = null,
        RequestOptions options = null
    ) => command.RespondEphemeralPingAsync(text, embeds, isTts, component, embed, options).Wait();

    /// <summary>
    /// Respond to the specified <see cref="SocketCommandBase"/> or
    /// follow up if it has already been acknowledged.
    /// </summary>
    /// <remarks>This will make it so that everyone can see the message.</remarks>
    /// <param name="command">The <see cref="SocketCommandBase"/></param>
    /// <param name="text">The text.</param>
    /// <param name="pingUser">Should the user be pinged?</param>
    /// <param name="embeds">The <see cref="Embed"/>s</param>
    /// <param name="isTts">Is the mssage a TTS message?</param>
    /// <param name="component">The <see cref="MessageComponent"/></param>
    /// <param name="embed">The <see cref="Embed"/></param>
    /// <param name="options">The <see cref="RequestOptions"/></param>
    public static void RespondPublic(
        this SocketCommandBase command,
        string text = null,
        bool pingUser = false,
        Embed[] embeds = null,
        bool isTts = false,
        MessageComponent component = null,
        Embed embed = null,
        RequestOptions options = null
    ) => command.RespondPublicAsync(text, pingUser, embeds, isTts, component, embed, options).Wait();

    /// <summary>
    /// Rejects if the <see cref="IUser"/> of the <see cref="SocketCommandBase"/> is not an admin.
    /// </summary>
    /// <param name="cmd">The <see cref="SocketCommandBase"/></param>
    /// <returns>True if the user has access.</returns>
    public static async Task<bool> RejectIfNotAdminAsync(this SocketCommandBase cmd)
        => await AdminUtility.RejectIfNotAdminAsync(cmd);

    /// <summary>
    /// Rejects if the <see cref="IUser"/> of the <see cref="SocketCommandBase"/> is not thr owner.
    /// </summary>
    /// <param name="cmd">The <see cref="SocketCommandBase"/></param>
    /// <returns>True if the user has access.</returns>
    public static async Task<bool> RejectIfNotOwnerAsync(this SocketCommandBase cmd)
        => await AdminUtility.RejectIfNotOwnerAsync(cmd);

  
    /// <summary>
    /// Defers the <see cref="SocketCommandBase"/> until a response is made.
    /// </summary>
    /// <remarks>This only exists to wrap an <see cref="IDisposable"/> to be used in using statements.</remarks>
    /// <param name="command">The <see cref="SocketCommandBase"/></param>
    /// <param name="options">The <see cref="RequestOptions"/></param>
    /// <returns></returns>
    public static async Task<IDisposable> DeferPublicAsync(this SocketCommandBase command, RequestOptions options = null)
    {
        await command.DeferAsync(false, options);

        return DiscardDisposable.Instance;
    }
}

#endif
