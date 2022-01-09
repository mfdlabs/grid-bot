using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class GetAllJobs : IStateSpecificCommandHandler
    {
        public string CommandName => "Get All Grid Server Jobs";
        public string CommandDescription => "Attempts to list all the grid server jobs as a JSON string.";
        public string[] CommandAliases => new[] { "gaj", "getalljobs" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            var jobs = await GridServerArbiter.Singleton.GetAllJobsExAsync();

            await message.ReplyAsync(jobs.Length == 0 ? "There are currently no jobs open." : jobs.ToJson());
        }
    }
}
