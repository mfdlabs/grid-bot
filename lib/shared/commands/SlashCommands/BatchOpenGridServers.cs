#if WE_LOVE_EM_SLASH_COMMANDS

using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Grid.Bot.Extensions;
using Grid.Bot.Interfaces;
using Grid.Bot.Utility;
using Reflection.Extensions;

namespace Grid.Bot.SlashCommands
{
    internal sealed class BatchOpenGridServers : IStateSpecificSlashCommandHandler
    {
        public string CommandDescription => "Attempts to batch open grid servers";
        public string CommandAlias => "batchinstance";
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;
        public ulong? GuildId => null;
        public SlashCommandOptionBuilder[] Options => new[]
        {
            new SlashCommandOptionBuilder()
                .WithName("count")
                .WithDescription("The count of grid servers to queue up. Defaults to 1. Max 25")
                .WithType(ApplicationCommandOptionType.Integer)
                .WithMinValue(1)
                .WithMaxValue(25)
        };

        public async Task Invoke(SocketSlashCommand command)
        {
            if (!await command.RejectIfNotAdminAsync()) return;

            var countParamValue = command.Data.GetOptionValue("count");

            var count = countParamValue != null ? countParamValue.ToInt32() : 1;

            if (count < 1)
            {
                await command.RespondEphemeralPingAsync("The instance count is required to be above 0.");
                return;
            }

            GridServerArbiter.Singleton.BatchCreateLeasedInstances(count: count);


            await command.RespondEphemeralPingAsync($"Successfully opened {count} of grid server instances.");
        }
    }
}

#endif
