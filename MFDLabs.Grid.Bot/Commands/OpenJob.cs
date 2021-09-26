using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class OpenJob : IStateSpecificCommandHandler
    {
        public string CommandName => "Open Grid Server Job";
        public string CommandDescription => $"Attempts to open a job on the Grid Server via SoapUtility\nLayout: {Settings.Singleton.Prefix}openjob jobID placeID?=1818 universeID?=1.";
        public string[] CommandAliases => new string[] { "oj", "openjob" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            var jobID = messageContentArray.ElementAtOrDefault(0);
            if (!long.TryParse(messageContentArray.ElementAtOrDefault(1), out var placeID)) placeID = 1818;
            if (!long.TryParse(messageContentArray.ElementAtOrDefault(2), out var universeID)) universeID = 1;

            if (jobID == default)
            {
                await message.ReplyAsync($"Missing required parameter 'jobId', the layout is: {Settings.Singleton.Prefix}{originalCommand} jobID placeID?=1818 universeID?=1`");
                return;
            }

            await GridServerCommandUtility.Singleton.LaunchSimpleGameAsync(jobID, placeID, universeID);
            await message.ReplyAsync($"Successfully opened job '{jobID}' with placeID {placeID} and universeID {universeID}.");
        }
    }
}
