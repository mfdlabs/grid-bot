using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Commands
{
    internal sealed class PrefixCheck : IStateSpecificCommandHandler
    {
        public string CommandName => "Check Prefix";

        public string CommandDescription => "Checks the current environment prefix";

        public string[] CommandAliases => new string[] { "pr" };

        public bool Internal => false;

        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            await message.ReplyAsync($"The current prefix is {Settings.Singleton.Prefix}");
        }
    }
}
