/* Copyright MFDLABS Corporation. All rights reserved. */

#if WE_LOVE_EM_SLASH_COMMANDS && SLASH_COMMANDS_USE_HELP_COMMAND

using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Registries;

/** TODO: Add functionality for Querying State Specific commands here? **/

namespace MFDLabs.Grid.Bot.SlashCommands
{
    [Obsolete("This is obsolete as slash commands have their own respected documentation already.")]
    internal sealed class Help : IStateSpecificSlashCommandHandler
    {
        public string CommandDescription => "Gets help on a specific command or all comands.";
        public string CommandAlias => "help";
        public bool Internal => false;
        // Set this if we want legacy help messages
        public bool IsEnabled { get; set; } = false;
        public ulong? GuildId => null;
        public SlashCommandOptionBuilder[] Options => new[]
        {
            // TODO: Make this list all the current command names so we can have it as a range operator
            new SlashCommandOptionBuilder()
                .WithName("command_name")
                .WithDescription("The optional command name to query with.")
                .WithType(ApplicationCommandOptionType.String)
                .WithRequired(false)
        };

        public async Task Invoke(SocketSlashCommand command)
        {
            var commandName = command.Data.GetOptionByName("command_name");

            if (commandName != null)
            {
                var embed = CommandRegistry.ConstructHelpEmbedForSingleSlashCommand((string)commandName.Value, command.User);

                if (embed == null)
                {
                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.IsAllowedToEchoBackNotFoundCommandException)
                    {
                        await command.RespondEphemeralPingAsync($"The command with the name '{commandName}' was not found.");
                    }
                    return;
                }

                await command.RespondEphemeralAsync(embed: embed);
                return;
            }

            var allCommandsEmbeds = CommandRegistry.ConstructHelpEmbedForAllSlashCommands(command.User);

            var count = 0;
            foreach (var embed in allCommandsEmbeds) count += embed.Fields.Length;

            await command.RespondEphemeralPingAsync($"Returning information on {count} commands.");

            foreach (var embed in allCommandsEmbeds)
                await command.RespondEphemeralAsync(embed: embed);
        }
    }
}

#endif