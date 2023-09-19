#if WE_LOVE_EM_SLASH_COMMANDS

namespace Grid.Bot.SlashCommands;

using System.IO;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using Extensions;
using Interfaces;

/// <summary>
/// Gets the current deployment ID.
/// </summary>
internal sealed class DeploymentId : ISlashCommandHandler
{
    /// <inheritdoc cref="ISlashCommandHandler.Description"/>
    public string Description => "Get Deployment ID";

    /// <inheritdoc cref="ISlashCommandHandler.Name"/>
    public string Name => "deployment";

    /// <inheritdoc cref="ISlashCommandHandler.IsInternal"/>
    public bool IsInternal => false;

    /// <inheritdoc cref="ISlashCommandHandler.IsEnabled"/>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc cref="ISlashCommandHandler.Options"/>
    public SlashCommandOptionBuilder[] Options => null;

    /// <inheritdoc cref="ISlashCommandHandler.ExecuteAsync(SocketSlashCommand)"/>
    public async Task ExecuteAsync(SocketSlashCommand command)
    {
        // The deployment ID is literally just the name of the current directory that the executable is in.
        // This is not a great way to do this, but it's the best I can think of for now.

        // Fetch current directory name.
        var currentDirectory = Directory.GetCurrentDirectory();
        var currentDirectoryName = Path.GetFileName(currentDirectory);

        // Reply with the deployment ID.
        await command.RespondEphemeralAsync(
            $"The deployment ID for this instance is: `{currentDirectoryName}`\n" +
            "Please paste this into the `Deployment ID` field in grid-bot-support " +
            "templates so that the internal team can easily identify this instance."
        );
    }
}

#endif
