using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;
using System.Linq;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class CloseJob : IStateSpecificCommandHandler
    {
        public string CommandName => "Close Job";

        public string CommandDescription => "Closes a specific grid server job by jobID.";

        public string[] CommandAliases => new string[] { "cj", "closejob" };

        public bool Internal => true;

        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            var jobID = messageContentArray.ElementAtOrDefault(0);

            if (jobID == default)
            {
                await message.ReplyAsync($"Missing required parameter 'jobId', the layout is: ${Settings.Singleton.Prefix}{originalCommand} jobID");
                return;
            }

            await SoapUtility.Singleton.CloseJobAsync(jobID);
            await message.ReplyAsync($"Successfully closed grid server jobs '{jobID}'.");
        }
    }
}
