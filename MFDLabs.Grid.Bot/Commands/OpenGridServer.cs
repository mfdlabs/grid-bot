﻿using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;

namespace MFDLabs.Grid.Bot.Commands
{
    internal sealed class OpenGridServer : IStateSpecificCommandHandler
    {
        public string CommandName => "Open Grid Server";
        public string CommandDescription => $"Attempts to open the grid server via '{Settings.Singleton.GridServerDeployerExecutableName}', if the deployer fails it will return info on why it failed.";
        public string[] CommandAliases => new string[] { "ogsrv", "opengridserver" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            var tto = SystemUtility.Singleton.OpenGridServer();

            await message.ReplyAsync($"Successfully opened grid server in '{tto.TotalSeconds}' seconds!");
        }
    }
}
