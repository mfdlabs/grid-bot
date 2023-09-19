namespace Grid.Bot.Utility;

using System.Threading.Tasks;

using Discord;
using Discord.Rest;
using Discord.WebSocket;

internal static class ReplyExtensions
{
    public static async Task<RestUserMessage> ReplyAsync(
        this SocketMessage message,
        string text = null,
        bool isTts = false,
        Embed embed = null,
        RequestOptions options = null
    ) => await message.Channel.SendMessageAsync(
                text,
                isTts,
                embed,
                options,
                new AllowedMentions(AllowedMentionTypes.Users) { MentionRepliedUser = true },
                new MessageReference(message.Id)
            );

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

    public static async Task RespondEphemeralPingAsync(
        this SocketCommandBase command,
        string text = null,
        Embed[] embeds = null,
        bool isTts = false,
        MessageComponent component = null,
        Embed embed = null,
        RequestOptions options = null
    ) => await command.RespondEphemeralAsync(text, true, embeds, isTts, component, embed, options);
}
