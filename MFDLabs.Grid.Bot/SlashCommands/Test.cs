using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Tasks;

namespace MFDLabs.Grid.Bot.SlashCommands
{
    internal sealed class TestCommand123 : IStateSpecificSlashCommandHandler
    {
        public string CommandName => "Get Prefix";
        public string CommandDescription => "Gets the current prefix from the settings file.";
        public string CommandAlias => "test123";
        public bool Internal => false;
        public bool IsEnabled { get; set; } = true;
        public ulong? GuildId { get; } = null;

        public SlashCommandOptionBuilder[] Options => new [] { new SlashCommandOptionBuilder().WithName("user_id").WithRequired(true).WithType(ApplicationCommandOptionType.Integer) };

        public Task Invoke(SocketSlashCommand command)
        {
            RenderQueueSlashCommandUserMetricsTask.Singleton.Port.Post(command);
            return Task.CompletedTask;
        }
    }
}
