using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Logging;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;

namespace MFDLabs.Grid.Bot.Commands
{
    internal sealed class LogFile : IStateSpecificCommandHandler
    {
        public string CommandName => "Get Log File Name";
        public string CommandDescription => "Fetches the log file name for the current instance.";
        public string[] CommandAliases => new[] { "log", "lfile", "logfile", "log-file-name", "log-file", "log-name" };
        public bool Internal => false;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            await message.ReplyAsync(
                $"The log file name for this instance is: `{SystemLogger.FileName}`\n" + 
                "Please paste this into the `Log File Name` field in grid-bot-support templates so that the internal team can easily identify this instance's log files."
            );
        }
    }
}