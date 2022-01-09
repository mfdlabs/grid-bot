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
        public string CommandDescription => $"Attempts to call a DiagEx SOAP action via the SoapUtility\nLayout:" +
                                            $"{MFDLabs.Grid.Bot.Properties.Settings.Default.Prefix}jobdiagnostics jobID type?=1.";
        public string[] CommandAliases => new[] { "jd", "jobdiag", "jobdiagnostics" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            var jobId = messageContentArray.ElementAtOrDefault(0);
            if (!int.TryParse(messageContentArray.ElementAtOrDefault(1), out int type)) type = 1;

            if (jobId == default)
            {
                await message.ReplyAsync($"Missing required parameter 'jobId', the layout is: " +
                                         $"{MFDLabs.Grid.Bot.Properties.Settings.Default.Prefix}{originalCommand} jobID type?=1");
                return;
            }

            await message.ReplyAsync(LuaUtility.ParseLuaValues(await GridServerArbiter.Singleton.DiagExAsync(type, jobId)));
        }
    }
}
