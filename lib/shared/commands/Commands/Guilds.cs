using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using Grid.Bot.Global;
using Grid.Bot.Interfaces;
using Grid.Bot.Extensions;

namespace Grid.Bot.Commands
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

            await message.ReplyAsync(
                $"We are in {BotRegistry.Client.Guilds.Count} guilds!"
            );
        }
    }
}
