/* Copyright MFDLABS Corporation. All rights reserved. */

#if WE_LOVE_EM_SLASH_COMMANDS

using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Registries;

namespace MFDLabs.Grid.Bot.SlashCommands
{
    internal sealed class CommandManagement : IStateSpecificSlashCommandHandler
    {
        public string CommandDescription => "Enables or disables a slash command";
        public string CommandAlias => "command";
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;
        public ulong? GuildId => null;
        public SlashCommandOptionBuilder[] Options => new[]
        {
            new SlashCommandOptionBuilder()
                .WithName("enable")
                .WithDescription("Try enable a SlashCommand")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("alias", ApplicationCommandOptionType.String, "The alias of the slash command to enable", true),
            new SlashCommandOptionBuilder()
                .WithName("disable")
                .WithDescription("Try disable a SlashCommand")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("alias", ApplicationCommandOptionType.String, "The alias of the slash command to disable", true)
                .AddOption("message", ApplicationCommandOptionType.String, "An optional message to echo back", false)
        };

        public async Task Invoke(SocketSlashCommand command)
        {
            if (!await command.RejectIfNotAdminAsync()) return;

            var subCommand = command.Data.GetSubCommand();
            var commandName = subCommand.GetOptionValue("alias").ToString();

            switch (subCommand.Name)
            {
                case "enable":
                    if (!CommandRegistry.SetIsSlashCommandEnabled(commandName, true))
                    {
                        await command.RespondEphemeralAsync($"The command '{commandName}' did not exist!");
                        return;
                    }

                    await command.RespondEphemeralAsync($"Successfully enabled command '{commandName}'");
                    return;
                case "disable":
                    var optionalMessage = subCommand.GetOptionValue("message")?.ToString();
                    if (!CommandRegistry.SetIsSlashCommandEnabled(commandName, true, optionalMessage))
                    {
                        await command.RespondEphemeralAsync($"The command '{commandName}' did not exist!");
                        return;
                    }

                    await command.RespondEphemeralAsync($"Successfully disabled command '{commandName}' for '{optionalMessage ?? "No reason!"}'");
                    return;

            }
        }
    }
}

#endif