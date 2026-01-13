namespace Grid.Bot;

/// <summary>
/// Settings provider for all commands related stuff.
/// </summary>
public class CommandsSettings : BaseSettingsProvider
{
    /// <inheritdoc cref="Configuration.IVaultProvider.Path"/>
    public override string Path => SettingsProvidersDefaults.CommandsPath;

    /// <summary>
    /// Determines whether lockdown commands are enabled.
    /// </summary>
    public bool EnableLockdownCommands => GetOrDefault(
        nameof(EnableLockdownCommands),
        true
    );

    /// <summary>
    /// Gets the ID of the guild to use for lockdown commands.
    /// </summary>
    public ulong LockdownGuildId => GetOrDefault(
        nameof(LockdownGuildId),
        0UL
    );

    /// <summary>
    /// Gets the command prefix.
    /// </summary>
    public string Prefix => GetOrDefault(
        nameof(Prefix),
        ">"
    );

    /// <summary>
    /// The text to respond with when commands are disabled and the <see cref="ShouldWarnWhenCommandsAreDisabled"/> is true.
    /// </summary>
    public string TextCommandsDisabledWarningText => GetOrDefault(
        nameof(TextCommandsDisabledWarningText),
        string.Empty
    );

    /// <summary>
    /// Determines whether text based commands should be enabled.
    /// </summary>
    /// <remarks>
    /// If this is disabled, the commands module will not be loaded, therefore this cannot
    /// be used to dynamically load commands.
    /// 
    /// If you wish to consume commands, then this must be enabled before the last shard
    /// within the connection pool has completed its initialization phase.
    /// 
    /// If <see cref="ShouldWarnWhenCommandsAreDisabled"/> is false and this is false, then the bot
    /// will not respond to any text based commands whatever. Otherwise, it will respond with the text
    /// defined in <see cref="TextCommandsDisabledWarningText"/> on each subsequent attempt of text
    /// usage.
    /// </remarks>
    public bool EnableTextCommands => GetOrDefault(
        nameof(EnableTextCommands),
        true
    );

    /// <summary>
    /// Determines whether the bot should warn the user when commands are disabled.
    /// </summary>
    /// <remarks>
    /// This is only applicable when <see cref="EnableTextCommands"/> is false.
    /// </remarks>
    public bool ShouldWarnWhenCommandsAreDisabled => GetOrDefault(
        nameof(ShouldWarnWhenCommandsAreDisabled),
        true
    );
}
