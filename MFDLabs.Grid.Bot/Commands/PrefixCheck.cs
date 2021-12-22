using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;

namespace MFDLabs.Grid.Bot.Commands
{
    internal sealed class PrefixCheck : IStateSpecificCommandHandler
    {
        public string CommandName => "Get Prefix";
        public string CommandDescription => "Gets the current prefix from the settings file.";
        public string[] CommandAliases => new[] { "pr", "prefix" };
        public bool Internal => false;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand) 
            => await message.ReplyAsync($"The current prefix is {MFDLabs.Grid.Bot.Properties.Settings.Default.Prefix}");
    }
}
