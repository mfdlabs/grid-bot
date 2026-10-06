namespace Grid.Bot;

/// <summary>
/// Options for all commands related settings.
/// </summary>
public class CommandsOptions
{
    /// <summary>
    /// The configuration section these options bind from.
    /// </summary>
    public const string SectionName = "Commands";

    /// <summary>
    /// The vault path for the commands options.
    /// </summary>
    public const string VaultPath = "commands";

    /// <summary>
    /// Gets or sets a value indicating whether lockdown commands are enabled.
    /// </summary>
    public bool EnableLockdownCommands { get; set; } = true;

    /// <summary>
    /// Gets or sets the ID of the guild to use for lockdown commands.
    /// </summary>
    public ulong LockdownGuildId { get; set; }

    /// <summary>
    /// Gets or sets the command prefix.
    /// </summary>
    public string Prefix { get; set; } = ">";

    /// <summary>
    /// Gets or sets the text to respond with when commands are disabled and <see cref="ShouldWarnWhenCommandsAreDisabled"/> is true.
    /// </summary>
    public string TextCommandsDisabledWarningText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether text based commands are enabled.
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
    public bool EnableTextCommands { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the bot warns the user when commands are disabled.
    /// </summary>
    /// <remarks>
    /// This is only applicable when <see cref="EnableTextCommands"/> is false.
    /// </remarks>
    public bool ShouldWarnWhenCommandsAreDisabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the feature gate for the EvaluateCSharp command.
    /// If this is false, the command is unusable regardless of role permissions.
    /// If this is true, the command is usable by the owner role only.
    /// </summary>
    public bool EvaluateCSharpCommandEnabled { get; set; } = true;
}
