#if WE_LOVE_EM_SLASH_COMMANDS

using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Logging;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;

namespace MFDLabs.Grid.Bot.SlashCommands
{
    internal sealed class LogFile : IStateSpecificSlashCommandHandler
    {
        public string CommandDescription => "Get Log File Name";
        public string CommandAlias => "log-file-name";
        public bool Internal => false;
        public bool IsEnabled { get; set; } = true;
        public ulong? GuildId => null;
        public SlashCommandOptionBuilder[] Options => null;

        public async Task Invoke(SocketSlashCommand command)
        {
            using (await command.DeferEphemeralAsync())
            {
                await command.RespondEphemeralAsync($"The log file name for this instance is: `{SystemLogger.FileName}`\nPlease paste this into the `Log File Name` field in grid-bot-support templates to that the internal team can easily identify this instance's log files.");
            }
        }
    }
}

#endif