using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class GetJobDiagnostics : IStateSpecificCommandHandler
    {
        public string CommandName => "Get Grid Server Job Diagnostics";
        public string CommandDescription => $"Attempts to call a DiagEx SOAP action via the SoapUtility\nLayout: {Settings.Singleton.Prefix}jobdiagnostics jobID type?=1.";
        public string[] CommandAliases => new string[] { "jd", "jobdiag", "jobdiagnostics" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            var jobID = messageContentArray.ElementAtOrDefault(0);
            if (!int.TryParse(messageContentArray.ElementAtOrDefault(1), out int type)) type = 1;

            if (jobID == default)
            {
                await message.ReplyAsync($"Missing required parameter 'jobId', the layout is: {Settings.Singleton.Prefix}{originalCommand} jobID type?=1");
                return;
            }

            await message.ReplyAsync(LuaUtility.Singleton.ParseLuaValues(await SoapUtility.Singleton.DiagExAsync(type, jobID)));
        }
    }
}
