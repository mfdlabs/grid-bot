﻿#if WE_LOVE_EM_SLASH_COMMANDS

using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;

namespace MFDLabs.Grid.Bot.SlashCommands
{
    internal sealed class DeploymentId : IStateSpecificSlashCommandHandler
    {
        public string CommandDescription => "Get Deployment ID";
        public string CommandAlias => "deployment";
        public bool Internal => false;
        public bool IsEnabled { get; set; } = true;
        public ulong? GuildId => null;
        public SlashCommandOptionBuilder[] Options => null;

        public async Task Invoke(SocketSlashCommand command)
        {
            using (await command.DeferEphemeralAsync())
            {
                // The deployment ID is literally just the name of the current directory that the executable is in.
                // This is not a great way to do this, but it's the best I can think of for now.
                
                // Fetch current directory name.
                var currentDirectory = Directory.GetCurrentDirectory();
                var currentDirectoryName = Path.GetFileName(currentDirectory);

                // Reply with the deployment ID.
                await command.RespondEphemeralAsync(
                    $"The deployment ID for this instance is: `{currentDirectoryName}`\n" + 
                    "Please paste this into the `Deployment ID` field in grid-bot-support templates so that the internal team can easily identify this instance."
                );
            }
        }
    }
}

#endif