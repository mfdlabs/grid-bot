﻿using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;

namespace MFDLabs.Grid.Bot.Commands
{
    internal sealed class RestartBot : IStateSpecificCommandHandler
    {
        public string CommandName => "Restart Bot Instance";
        public string CommandDescription => "Restarts the bot instance via invoking a SIGUSR2 event.";
        public string[] CommandAliases => new[] { "re", "restart" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            await message.ReplyAsync("restarting bot and event lifetime.");

            SignalUtility.InvokeUserSignal2();
        }
    }
}
