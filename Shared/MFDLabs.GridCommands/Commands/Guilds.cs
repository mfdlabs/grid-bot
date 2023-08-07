using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Global;
using MFDLabs.Grid.Bot.Interfaces;

namespace MFDLabs.Grid.Bot.Commands
{
    internal sealed class Guilds : IStateSpecificCommandHandler
    {
        public string CommandName => "Get Guilds";
        public string CommandDescription => "Gets the current bot's guilds";
        public string[] CommandAliases => new[] { "guilds", "servers" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            var guilds = BotRegistry.Client.Guilds;

            var returnString = "";

            var guildNames = guilds.Select(g => g.Name);

            foreach (var name in guildNames)
                returnString += $"{name}\n";

            await message.ReplyAsync(
                $"We are in {guilds.Count} guilds!",
                embed: new EmbedBuilder()
                    .WithTitle("Guild Names")
                    .WithCurrentTimestamp()
                    .WithColor(0x00, 0xff, 0x00)
                    .WithDescription($"```\n{returnString}```")
                    .Build()
            );
        }
    }
}
