namespace Grid.Bot.Commands;

using System;
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
    /// <param name="message">The message text</param>
    /// <param name="embed">An optional <see cref="Embed"/></param>
    /// <returns>An awaitable task.</returns>
    public static async Task RespondAsync(
        this IUnifiedCommandContext context,
        string message = null,
        Embed embed = null)
    {
        if (context.IsInteraction)
        {
            await context.Interaction.FollowupAsync(text: message, embed: embed).ConfigureAwait(false);

            return;
        }

        await context.Message.Channel.SendMessageAsync(
            text: message,
            embed: embed,
            messageReference: new(messageId: context.Message.Id)
        ).ConfigureAwait(false);
    }
}