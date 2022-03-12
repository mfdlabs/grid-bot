using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;

namespace MFDLabs.Grid.Bot.Commands
{
    internal sealed class DeploymentId : IStateSpecificCommandHandler
    {
        public string CommandName => "Get Deployment ID";
        public string CommandDescription => "Fetches the deployment ID for the current instance.";
        public string[] CommandAliases => new[] { "dep", "dply", "deploy", "depid", "deployment", "deploymentid" };
        public bool Internal => false;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            // The deployment ID is literally just the name of the current directory that the executable is in.
            // This is not a great way to do this, but it's the best I can think of for now.
            
            // Fetch current directory name.
            var currentDirectory = Directory.GetCurrentDirectory();
            var currentDirectoryName = Path.GetFileName(currentDirectory);

            // Reply with the deployment ID.
            await message.ReplyAsync($"The deployment ID for this instance is: `{currentDirectoryName}`\nPlease paste this into the `Deployment ID` field in grid-bot-support templates to that the internal team can easily identify this instance.");
        }
    }
}