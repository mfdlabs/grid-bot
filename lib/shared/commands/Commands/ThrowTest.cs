using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Grid.Bot.Extensions;
using Grid.Bot.Interfaces;

namespace Grid.Bot.Commands
{
    internal class ThrowTest : IStateSpecificCommandHandler
    {
        public string CommandName => "Throw Tester";
        public string CommandDescription => $"Throws an ApplicationException with the given text (if any)." +
                                            $"Used as a test for the exception logger\nLayout: " +
                                            $"{Grid.Bot.Properties.Settings.Default.Prefix}throw ...message?";
        public string[] CommandAliases => new[] { "throw" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            throw new ApplicationException(messageContentArray.Length > 0
                ? string.Join(" ",
                    messageContentArray)
                : "Exception handler test.");
        }
    }
}
