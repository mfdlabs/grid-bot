using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class CloseExpiredJobs : IStateSpecificCommandHandler
    {
        public string CommandName => "Close All Expired Jobs";

        public string CommandDescription => "Closes all the grid server's expired jobs.";

        public string[] CommandAliases => new string[] { "caej", "closeallexpiredjobs" };

        public bool Internal => true;

        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            await SoapUtility.Singleton.CloseExpiredJobsAsync();
            await message.ReplyAsync("Successfully closed all expired grid server jobs.");
        }
    }
}
