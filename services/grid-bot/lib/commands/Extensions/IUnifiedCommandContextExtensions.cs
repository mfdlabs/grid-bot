namespace Grid.Bot.Commands;

using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.Commands;

using Grid.Bot.Telemetry;

/// <summary>
/// Extensions for <see cref="IUnifiedCommandContext"/>
/// </summary>
public static class IUnifiedCommandContextExtensions
{
    /// <summary>
    /// Gets the first attachment from the message, if any. Returns null for interactions.
    /// </summary>
    /// <remarks>
    /// Only relevant for message-based commands; interactions provide attachments via arguments.
    /// </remarks>
    /// <param name="context">The unified command context.</param>
    /// <returns>The first attachment from the message, or null if there are no attachments or if the context is an interaction.</returns>
    public static IAttachment GetAttachment(this ICommandContext context)
        => context.Message.Attachments.FirstOrDefault();

    private static Task TraceRespondAsync(IUnifiedCommandContext context, Func<Task> respond)
        => GridBotTelemetry.RunAsync(
            GridBotTelemetry.Spans.DiscordRespond,
            respond,
            ActivityKind.Client,
            activity => activity.SetTag(GridBotTelemetry.Tags.RespondTarget, context.IsInteraction ? "interaction" : "message")
        );

    /// <summary>
    /// Responds to a message or interaction.
    /// </summary>
    /// <param name="context">The <see cref="IUnifiedCommandContext"/></param>
    /// <param name="text">The message text</param>
    /// <param name="embed">An optional <see cref="Embed"/></param>
    /// <param name="embeds">An optional array of <see cref="Embed"/></param>
    /// <returns>An awaitable task.</returns>
    public static Task RespondAsync(
        this IUnifiedCommandContext context,
        string text = null,
        Embed embed = null,
        Embed[] embeds = null
    ) => TraceRespondAsync(context, () => RespondCoreAsync(context, text, embed, embeds));

    private static async Task RespondCoreAsync(
        IUnifiedCommandContext context,
        string text,
        Embed embed,
        Embed[] embeds
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
    public static Task RespondWithFileAsync(
        this IUnifiedCommandContext context,
        Stream fileStream,
        string fileName,
        string text = null,
        Embed embed = null,
        Embed[] embeds = null
    ) => TraceRespondAsync(context, () => RespondWithFileCoreAsync(context, fileStream, fileName, text, embed, embeds));

    private static async Task RespondWithFileCoreAsync(
        IUnifiedCommandContext context,
        Stream fileStream,
        string fileName,
        string text,
        Embed embed,
        Embed[] embeds
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

    /// <summary>
    /// Responds to a message or interaction with multiple file attachments.
    /// </summary>
    /// <param name="context">The <see cref="IUnifiedCommandContext"/></param>
    /// <param name="attachments">The file attachments to include in the response</param>
    /// <param name="text">The message text</param>
    /// <param name="embed">An optional <see cref="Embed"/></param>
    /// <param name="embeds">An optional array of <see cref="Embed"/></param>
    /// <returns>An awaitable task.</returns>
    public static Task RespondWithFilesAsync(
        this IUnifiedCommandContext context,
        IEnumerable<FileAttachment> attachments,
        string text = null,
        Embed embed = null,
        Embed[] embeds = null
    ) => TraceRespondAsync(context, () => RespondWithFilesCoreAsync(context, attachments, text, embed, embeds));

    private static async Task RespondWithFilesCoreAsync(
        IUnifiedCommandContext context,
        IEnumerable<FileAttachment> attachments,
        string text,
        Embed embed,
        Embed[] embeds
    )
    {
        if (context.IsInteraction)
        {
            await context.Interaction.FollowupWithFilesAsync(
                attachments,
                text: text,
                embed: embed,
                embeds: embeds
            ).ConfigureAwait(false);

            return;
        }

        await context.Message.Channel.SendFilesAsync(
            attachments,
            text: text,
            embed: embed,
            embeds: embeds,
            messageReference: new(messageId: context.Message.Id)
        ).ConfigureAwait(false);
    }
}