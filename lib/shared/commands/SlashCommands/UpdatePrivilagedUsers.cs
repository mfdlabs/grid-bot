#if WE_LOVE_EM_SLASH_COMMANDS

namespace Grid.Bot.SlashCommands;

using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using Extensions;
using Interfaces;

/// <summary>
/// Manages the user privilages.
/// </summary>
internal sealed class UpdatePrivilagedUsers : ISlashCommandHandler
{
    /// <inheritdoc cref="ISlashCommandHandler.Description"/>
    public string Description => "Adds or removes a user to/from the privilaged user list.";

    /// <inheritdoc cref="ISlashCommandHandler.Name"/>
    public string Name => "privilaged_users";

    /// <inheritdoc cref="ISlashCommandHandler.IsInternal"/>
    public bool IsInternal => true;

    /// <inheritdoc cref="ISlashCommandHandler.IsEnabled"/>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc cref="ISlashCommandHandler.Options"/>
    public SlashCommandOptionBuilder[] Options => new[]
    {
        new SlashCommandOptionBuilder()
            .WithName("add")
            .WithDescription("Add user to the privilaged user list")
            .WithType(ApplicationCommandOptionType.SubCommand)
            .AddOption("user", ApplicationCommandOptionType.User, "The user to add", true),

        new SlashCommandOptionBuilder()
            .WithName("remove")
            .WithDescription("Remove user from the privilaged user list")
            .WithType(ApplicationCommandOptionType.SubCommand)
            .AddOption("user", ApplicationCommandOptionType.User, "The user to remove", true)
    };

    /// <inheritdoc cref="ISlashCommandHandler.ExecuteAsync(SocketSlashCommand)"/>
    public async Task ExecuteAsync(SocketSlashCommand command)
    {
        if (!await command.RejectIfNotAdminAsync()) return;

        var subCommand = command.Data.GetSubCommand();

        var userOption = subCommand.GetOptionValue<IUser>("user");
        if (userOption == null)
        {
            await command.RespondEphemeralPingAsync("The user cannot be null");

            return;
        }

        if (userOption.IsBot || userOption.IsWebhook)
        {
            await command.RespondEphemeralPingAsync("Cannot update the status of a bot user.");

            return;
        }

        if (userOption.IsOwner())
        {
            await command.RespondEphemeralPingAsync("Cannot update the status of user because they are the owner.");

            return;
        }


        switch (subCommand.Name)
        {
            case "add":
                if (userOption.IsPrivilaged())
                {
                    await command.RespondEphemeralPingAsync($"The user '{userOption.Id}' is already on the privilaged user list!");

                    return;
                }

                userOption.Entitle();

                await command.RespondEphemeralPingAsync($"Successfully added '{userOption.Id}' to the privilaged user list!");

                return;
            case "remove":
                if (!userOption.IsPrivilaged())
                {
                    await command.RespondEphemeralPingAsync($"The user '{userOption.Id}' is not on the privilaged user list!");

                    return;
                }

                userOption.Disentitle();

                await command.RespondEphemeralPingAsync($"Successfully removed '{userOption.Id}' from the privilaged user list!");

                return;
        }
    }
}

#endif
