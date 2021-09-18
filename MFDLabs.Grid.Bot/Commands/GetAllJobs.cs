using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;
using MFDLabs.Text.Extensions;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class GetAllJobs : IStateSpecificCommandHandler
    {
        public string CommandName => "Get All Jobs";

        public string CommandDescription => "Gets all the grid server's jobs.";

        public string[] CommandAliases => new string[] { "gaj", "getalljobs" };

        public bool Internal => true;

        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            var jobs = await SoapUtility.Singleton.GetAllJobsExAsync();

            await message.ReplyAsync(jobs.Length == 0 ? "There are currently no jobs open." : jobs.ToJson());
        }
    }
}
