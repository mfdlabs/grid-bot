using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class CloseJob : IStateSpecificCommandHandler
    {
        public string CommandName => "Close Grid Server Job";
        public string CommandDescription => $"Attempts to close a grid server job via the SoapUtility\nLayout: {MFDLabs.Grid.Bot.Properties.Settings.Default.Prefix}closejob jobID.";
        public string[] CommandAliases => new string[] { "cj", "closejob" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            var jobID = messageContentArray.ElementAtOrDefault(0);

            if (jobID == default)
            {
                await message.ReplyAsync($"Missing required parameter 'jobId', the layout is: {MFDLabs.Grid.Bot.Properties.Settings.Default.Prefix}{originalCommand} jobID");
                return;
            }

            await GridServerArbiter.Singleton.CloseJobAsync(jobID);
            await message.ReplyAsync($"Successfully closed grid server jobs '{jobID}'.");
        }
    }
}
