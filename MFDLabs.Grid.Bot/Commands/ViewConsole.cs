﻿using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Tasks;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class ViewConsole : IStateSpecificCommandHandler
    {
        public string CommandName => "View Grid Server Console";
        public string CommandDescription => "Dispatches a 'ScreenshotTask' request to the task thread port. Will try to screenshot the current grid server's console output.";
        public string[] CommandAliases => new string[] { "vc", "viewconsole" };
        public bool Internal => !Settings.Singleton.ViewConsoleEnabled;
        public bool IsEnabled { get; set; } = true;

        public Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            ScreenshotTask.Singleton.Port.Post(message);
            return Task.CompletedTask;
        }
    }
}
