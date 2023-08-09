using System.Threading.Tasks;
using Discord.WebSocket;
using Grid.Bot.Extensions;
using Grid.Bot.Hooks;
using Grid.Bot.Interfaces;
using Grid.Bot.Utility;

namespace Grid.Bot.Commands
{
    internal class Exit : IStateSpecificCommandHandler
    {
        public string CommandName => "Exit Exclusive";
        public string CommandDescription => $"If the setting 'KillCommandShouldForce' is enabled, " +
                                            $"it will invoke the '{typeof(SuicideHook).FullName}' to force " +
                                            $"a shutdown (No cleanup).";
        public string[] CommandAliases => new[] { "911", "panic" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            await message.ReplyAsync("911! Invoking suicide request!");

            if (global::Grid.Bot.Properties.Settings.Default.KillCommandShouldForce)
            {
                new SuicideHook().Callback('f');
                return;
            }

            SignalUtility.InvokeInteruptSignal();
        }
    }
}
