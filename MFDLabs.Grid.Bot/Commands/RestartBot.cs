using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Commands
{
    internal sealed class RestartBot : IStateSpecificCommandHandler
    {
        public string CommandName => "Restart Bot";

        public string CommandDescription => "Goes through a full shutdown sequence.";

        public string[] CommandAliases => new string[] { "re", "restart" };

        public bool Internal => true;

        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            await message.ReplyAsync("restarting bot and event lifetime.");
            SignalUtility.Singleton.InvokeUserSignal2(false);
            return;
        }
    }
}
