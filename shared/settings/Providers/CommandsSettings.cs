namespace Grid.Bot;

using System;

/// <summary>
/// Settings provider for all commands related stuff.
/// </summary>
public class CommandsSettings : BaseSettingsProvider
{
    /// <inheritdoc cref="Configuration.IVaultProvider.Path"/>
    public override string Path => SettingsProvidersDefaults.CommandsPath;

    /// <summary>
    /// Gets the command prefix.
    /// </summary>
    public string Prefix => GetOrDefault(
        nameof(Prefix),
        ">"
    );

    /// <summary>
    /// Gets the aliases for all the commands that were in the previous phase (text-based)
    /// </summary>
    /// <remarks>
    /// This is used to make sure that the bot doesn't respond to commands that were not meant for it.
    /// 
    /// This setting will be phased out in the future.
    /// </remarks>
    public string[] PreviousPhaseCommands => GetOrDefault(
        nameof(PreviousPhaseCommands),
        Array.Empty<string>()
    );
}
