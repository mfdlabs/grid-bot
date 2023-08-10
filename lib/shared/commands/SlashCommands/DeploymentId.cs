#if WE_LOVE_EM_SLASH_COMMANDS

using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Grid.Bot.Extensions;
using Grid.Bot.Interfaces;
using Grid.Bot.Utility;

namespace Grid.Bot.SlashCommands
{
    internal sealed class DeploymentId : IStateSpecificSlashCommandHandler
    {
        public string CommandDescription => "Get Deployment ID";
        public string Name => "deployment";
        public bool Internal => false;
        public bool IsEnabled { get; set; } = true;
        public SlashCommandOptionBuilder[] Options => null;

        public async Task Invoke(SocketSlashCommand command)
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

#endif
