/* Copyright MFDLABS Corporation. All rights reserved. */

#if WE_LOVE_EM_SLASH_COMMANDS

using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;
using MFDLabs.Reflection.Extensions;

namespace MFDLabs.Grid.Bot.SlashCommands
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
                .WithName("unsafe")
                .WithDescription("Should we queue up instances in an unsafe manner? Defaults to false.")
                .WithType(ApplicationCommandOptionType.Boolean)
                .WithRequired(false),
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

            using (await command.DeferEphemeralAsync())
            {

                if (global::MFDLabs.Grid.Properties.Settings.Default.SingleInstancedGridServer)
                {
                    await command.RespondEphemeralPingAsync("Not opening any instances due to single-instanced environment.");
                    return;
                }

                var unsafeParamValue = command.Data.GetOptionValue("unsafe");
                var countParamValue = command.Data.GetOptionValue("count");


                var @unsafe = unsafeParamValue != null && unsafeParamValue.ToBoolean();
                var count = countParamValue != null ? countParamValue.ToInt32() : 1;

                if (count < 1)
                {
                    await command.RespondEphemeralPingAsync("The instance count is required to be above 0.");
                    return;
                }

                if (@unsafe)
                    GridServerArbiter.Singleton.BatchQueueUpArbiteredInstancesUnsafe(count);
                else
                    GridServerArbiter.Singleton.BatchQueueUpArbiteredInstances(count);

                if (@unsafe)
                    await command.RespondEphemeralPingAsync($"Successfully enqueued {count} of grid server instances for immediate startup.");
                else
                    await command.RespondEphemeralPingAsync($"Successfully opened {count} of grid server instances.");
            }
        }
    }
}

#endif