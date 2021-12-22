using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class GetJobCount : IStateSpecificCommandHandler
    {
        public string CommandName => "Get Grid Server Job Count";
        public string CommandDescription => "Attempts to get the grid server's job count via the SoapUtility.";
        public string[] CommandAliases => new[] { "gjc", "getjobcount" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            var jobs = await GridServerArbiter.Singleton.GetAllJobsExAsync();

            await message.ReplyAsync(jobs.Length.ToString());
        }
    }
}
