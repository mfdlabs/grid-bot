#if WE_LOVE_EM_SLASH_COMMANDS

using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using Logging;

using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;

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
            await command.RespondEphemeralAsync(
                $"The log file name for this instance is: `{Logger.Singleton.FileName}`\n" +
                "Please paste this into the `Log File Name` field in grid-bot-support templates so that the internal team can easily identify this instance's log files."
            );
        }
    }
}

#endif
