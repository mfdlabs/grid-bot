#if WE_LOVE_EM_SLASH_COMMANDS

namespace Grid.Bot.SlashCommands;

using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using Extensions;
using Interfaces;
using Registries;

/// <summary>
/// Manages the state of <see cref="ISlashCommandHandler"/>
/// </summary>
internal sealed class CommandManagement : ISlashCommandHandler
{
    /// <inheritdoc cref="ISlashCommandHandler.Description"/>
    public string Description => "Enables or disables a slash command";

    /// <inheritdoc cref="ISlashCommandHandler.Name"/>
    public string Name => "command";

    /// <inheritdoc cref="ISlashCommandHandler.IsInternal"/>
    public bool IsInternal => true;

    /// <inheritdoc cref="ISlashCommandHandler.IsEnabled"/>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc cref="ISlashCommandHandler.Options"/>
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

    /// <inheritdoc cref="ISlashCommandHandler.ExecuteAsync(SocketSlashCommand)"/>
    public async Task ExecuteAsync(SocketSlashCommand command)
    {
        if (!await command.RejectIfNotAdminAsync()) return;

        var subCommand = command.Data.GetSubCommand();
        var commandName = subCommand.GetOptionValue<string>("alias");

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
                var optionalMessage = subCommand.GetOptionValue<string>("message");
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

#endif
