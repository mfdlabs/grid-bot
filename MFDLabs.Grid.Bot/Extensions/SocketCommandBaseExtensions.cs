using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace MFDLabs.Grid.Bot.Extensions
{
    internal static class SocketCommandBaseExtensions
    {
        public static async Task RespondEphemeralAsync(this SocketCommandBase command, string text = null, Embed[] embeds = null, bool isTTS = false, MessageComponent component = null, Embed embed = null, RequestOptions options = null)
        {
            if (!command.HasResponded)
            {
                await command.RespondAsync(text, embeds, isTTS, true, new AllowedMentions(AllowedMentionTypes.Users), component, embed, options);
                return;
            }

            await command.FollowupAsync(text, embeds, isTTS, true, new AllowedMentions(AllowedMentionTypes.Users), component, embed, options);
        }

        public static async Task<RestFollowupMessage> RespondWithFileEphemeralAsync(this SocketCommandBase command, Stream fileStream, string fileName, string text = null, Embed[] embeds = null, bool isTTS = false, MessageComponent components = null, Embed embed = null, RequestOptions options = null)
            => await command.FollowupWithFileAsync(fileStream, fileName, text, embeds, isTTS, true, new AllowedMentions(AllowedMentionTypes.Users), components, embed, options);

        public static RestFollowupMessage RespondWithFileEphemeral(this SocketCommandBase command, Stream fileStream, string fileName, string text = null, Embed[] embeds = null, bool isTTS = false, MessageComponent components = null, Embed embed = null, RequestOptions options = null)
            => command.RespondWithFileEphemeralAsync(fileStream, fileName, text, embeds, isTTS, components, embed, options).GetAwaiter().GetResult();

        public static void RespondEphemeral(this SocketCommandBase command, string text = null, Embed[] embeds = null, bool isTTS = false, MessageComponent component = null, Embed embed = null, RequestOptions options = null)
            => command.RespondEphemeralAsync(text, embeds, isTTS, component, embed, options).GetAwaiter().GetResult();
    }
}
