using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Global;
using MFDLabs.Text.Extensions;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;

namespace MFDLabs.Grid.Bot.Commands
{
    internal sealed class SendMessageInGuildChannel : IStateSpecificCommandHandler
    {
        public string CommandName => "Send Message In Guild Channel";
        public string CommandDescription => $"Sends a message to a guild channel\nLayout:" +
                                            $"{(global::MFDLabs.Grid.Bot.Properties.Settings.Default.Prefix)}message " +
                                            $"guildId channelId ...message&attachments.";
        public string[] CommandAliases => new[] { "msg", "message" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            var clientGuildId = messageContentArray.ElementAtOrDefault(0);
            if (clientGuildId.IsNullOrEmpty())
            {
                await message.ReplyAsync("The guild id is a required parameter.");
                return;
            }

            if (!ulong.TryParse(clientGuildId, out var guildId))
            {
                await message.ReplyAsync($"The guild id '{clientGuildId}' is not a valid '{typeof(ulong)}'.");
                return;
            }

            var clientChannelId = messageContentArray.ElementAtOrDefault(1);
            if (clientChannelId.IsNullOrEmpty())
            {
                await message.ReplyAsync("The channel id is a required parameter.");
                return;
            }

            if (!ulong.TryParse(clientChannelId, out var channelId))
            {
                await message.ReplyAsync($"The channel id '{clientChannelId}' is not a valid '{typeof(ulong)}'.");
                return;
            }

            var messageContent = messageContentArray.Skip(2).Take(messageContentArray.Length - 1).Join(" ");
            var fileContents = new Stream[message.Attachments.Count];
            if (messageContent.IsNullOrEmpty())
            {
                if (message.Attachments.Count == 0)
                {
                    await message.ReplyAsync("Expected the message to not be empty, or at least 1 attachment to be present.");
                    return;
                }
            }

            var i = 0;
            foreach (var attachment in message.Attachments)
            {
                fileContents[i] = new MemoryStream(attachment.GetRawAttachmentBuffer());
                i++;
            }

            var guild = BotRegistry.Client.GetGuild(guildId);
            if (guild == null)
            {
                await message.ReplyAsync($"Unknown guild '{guildId}'.");
                return;
            }

            var channel = guild.GetTextChannel(channelId);
            if (channel == null)
            {
                await message.ReplyAsync($"Unknown channel '{channelId}'.");
                return;
            }

            if (!messageContent.IsNullOrEmpty())
                await channel.SendMessageAsync(text: messageContent);

            if (fileContents.Length != 0)
            {
                var attachments = message.Attachments.ToArray();
                for (var j = 0; j < fileContents.Length; j++)
                {
                    var stream = fileContents[j];
                    await channel.SendFileAsync(stream, attachments[j].Filename, "");
                    stream.Dispose();
                }
            }
        }
    }
}
