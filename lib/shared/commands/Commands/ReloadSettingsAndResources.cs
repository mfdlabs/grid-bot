﻿using System.Reflection;
using System.Threading.Tasks;
using Discord.WebSocket;
using Grid.Bot.Extensions;
using Grid.Bot.Interfaces;

namespace Grid.Bot.Commands
{
    internal sealed class ReloadSettingsAndResources : IStateSpecificCommandHandler
    {
        public string CommandName => "Reload Bot Instance Settings and Resources";
        public string CommandDescription => "Attempts to reload the settings from the current application" +
                                            "domain's configuration file.";
        public string[] CommandAliases => new[] { "reload", "reloadsettings" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            global::Grid.Bot.Properties.Settings.Default.Reload();
            await message.ReplyAsync($"Successfully reloaded all settings " +
                                     $"from {Assembly.GetEntryAssembly().GetName().Name}.exe.config");
        }
    }
}
