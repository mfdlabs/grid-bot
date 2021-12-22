using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class CloseExpiredJobs : IStateSpecificCommandHandler
    {
        public string CommandName => "Close All Expired Grid Server Jobs";
        public string CommandDescription => "Attempts to close all of the grid server's expired jobs via the SoapUtility.";
        public string[] CommandAliases => new[] { "caej", "closeallexpiredjobs" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            await GridServerArbiter.Singleton.CloseExpiredJobsAsync();
            await message.ReplyAsync("Successfully closed all expired grid server jobs.");
        }
    }
}
