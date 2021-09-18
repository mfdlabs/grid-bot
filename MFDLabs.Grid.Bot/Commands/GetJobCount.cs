using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class GetJobCount : IStateSpecificCommandHandler
    {
        public string CommandName => "Get Job Count";

        public string CommandDescription => "Gets the count of grid server jobs.";

        public string[] CommandAliases => new string[] { "gjc", "getjobcount" };

        public bool Internal => true;

        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            var jobs = await SoapUtility.Singleton.GetAllJobsExAsync();

            await message.ReplyAsync(jobs.Length.ToString());
        }
    }
}
