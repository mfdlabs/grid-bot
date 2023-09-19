#if WE_LOVE_EM_SLASH_COMMANDS

namespace Grid.Bot.SlashCommands;

using System;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using Networking;

using Extensions;
using Interfaces;

/// <summary>
/// Gets the current machine hostname, machine ID and local IPv4.
/// </summary>
internal sealed class Hostname : ISlashCommandHandler
{
    /// <inheritdoc cref="ISlashCommandHandler.Description"/>
    public string Description => "Get Machine Host Name";

    /// <inheritdoc cref="ISlashCommandHandler.Name"/>
    public string Name => "hostname";

    /// <inheritdoc cref="ISlashCommandHandler.IsInternal"/>
    public bool IsInternal => false;

    /// <inheritdoc cref="ISlashCommandHandler.IsEnabled"/>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc cref="ISlashCommandHandler.Options"/>
    public SlashCommandOptionBuilder[] Options => null;

    /// <inheritdoc cref="ISlashCommandHandler.ExecuteAsync(SocketSlashCommand)"/>
    public async Task ExecuteAsync(SocketSlashCommand command)
    {
        await command.RespondEphemeralAsync(
            $"The hostname for this instance is: `{LocalIpAddressProvider.Singleton.GetHostName()}`\n" +
            $"The machine ID for this machine is: `{Environment.MachineName}`\n" +
            $"The IP address for this machine is: `{LocalIpAddressProvider.Singleton.AddressV4}`\n" +
            "Please paste this into the `Host Name` field in grid-bot-support templates so that the internal team can easily identify this instance."
        );
    }
}

#endif
