namespace Grid.Bot.Commands;

using System;
using System.IO;
using System.Threading.Tasks;

using Discord;

/// <summary>
/// Extensions for <see cref="IUnifiedCommandContext"/>
/// </summary>
public static class IUnifiedCommandContextExtensions
{
    /// <summary>
    /// Responds to a message or interaction.
    /// </summary>
    /// <param name="context">The <see cref="IUnifiedCommandContext"/></param>
    /// <param name="text">The message text</param>
    /// <param name="embed">An optional <see cref="Embed"/></param>
    /// <param name="embeds">An optional array of <see cref="Embed"/></param>
    /// <returns>An awaitable task.</returns>
    public static async Task RespondAsync(
        this IUnifiedCommandContext context,
        string text = null,
        Embed embed = null,
        Embed[] embeds = null
    )
    {
        if (context.IsInteraction)
        {
            await context.Interaction.FollowupAsync(
                text: text, 
                embed: embed,
                embeds: embeds
            ).ConfigureAwait(false);

            return;
        }

        await context.Message.Channel.SendMessageAsync(
            text: text,
            embed: embed,
            embeds: embeds,
            messageReference: new(messageId: context.Message.Id)
        ).ConfigureAwait(false);
    }

    /// <summary>
    /// Responds to a message or interaction with a file attachment.
    /// </summary>
    /// <param name="context">The <see cref="IUnifiedCommandContext"/></param>
    /// <param name="fileStream">The file stream to attach</param>
    /// <param name="fileName">The name of the file</param>
    /// <param name="text">The message text</param>
    /// <param name="embed">An optional <see cref="Embed"/></param>
    /// <param name="embeds">An optional array of <see cref="Embed"/></param>
    /// <returns>An awaitable task.</returns>
    public static async Task RespondWithFileAsync(
        this IUnifiedCommandContext context,
        Stream fileStream,
        string fileName,
        string text = null,
        Embed embed = null,
        Embed[] embeds = null
    )
    {
        if (context.IsInteraction)
        {
            await context.Interaction.FollowupWithFileAsync(
                fileStream, 
                fileName,
                text: text,
                embed: embed,
                embeds: embeds
            ).ConfigureAwait(false);

            return;
        }

        await context.Message.Channel.SendFileAsync(
            fileStream,
            fileName,
            text: text,
            embed: embed,
            embeds: embeds,
            messageReference: new(messageId: context.Message.Id)
        ).ConfigureAwait(false);
    }
}