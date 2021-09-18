using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Hooks;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class Exit : IStateSpecificCommandHandler
    {
        public string CommandName => "Exit";

        public string CommandDescription => "Invokes an exit request to exit the bot, to be used in severe situations!";

        public string[] CommandAliases => new string[] { "911", "panic" };

        public bool Internal => true;

        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            await message.ReplyAsync("911! Invoking suicide request!");

            if (Settings.Singleton.KillCommandShouldForce)
            {
                new SuicideHook().Callback('f');
                return;
            }

            SignalUtility.Singleton.InvokeInteruptSignal();
        }
    }
}
