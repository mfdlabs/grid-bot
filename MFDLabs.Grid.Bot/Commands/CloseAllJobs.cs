using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Commands
{
    internal sealed class CloseAllJobs : IStateSpecificCommandHandler
    {
        public string CommandName => "Close All Jobs";

        public string CommandDescription => "Closes all the grid server's jobs.";

        public string[] CommandAliases => new string[] { "caj", "closealljobs" };

        public bool Internal => true;

        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            await SoapUtility.Singleton.CloseAllJobsAsync();
            await message.ReplyAsync("Successfully closed all grid server jobs.");
        }
    }
}
