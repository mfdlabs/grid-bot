namespace Grid.Bot;

using System;

/// <summary>
/// Options for all script execution related settings.
/// </summary>
public class ScriptsOptions
{
    /// <summary>
    /// The configuration section these options bind from.
    /// </summary>
    public const string SectionName = "Scripts";

    /// <summary>
    /// The vault path for the scripts options.
    /// </summary>
    public const string VaultPath = "scripts";

    /// <summary>
    /// Gets or sets the max size for a script used by the Execute Script commands in KiB.
    /// </summary>
    public int ScriptExecutionMaxFileSizeKb { get; set; } = 75;

    /// <summary>
    /// Gets or sets the max size for the result file used by the Execute Script command in KiB.
    /// </summary>
    public int ScriptExecutionMaxResultSizeKb { get; set; } = 50;

    /// <summary>
    /// Gets or sets a value indicating whether the LuaVM is enabled.
    /// </summary>
    public bool LuaVMEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the percentage to use for logging scripts.
    /// </summary>
    public int ScriptLoggingPercentage { get; set; }

    /// <summary>
    /// Gets or sets a Discord webhook URL to send script logs to.
    /// </summary>
    public string ScriptLoggingDiscordWebhookUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the hashes of scripts that have already been logged and should not be logged again.
    /// </summary>
    /// <remarks>Written at runtime through <see cref="ISettingsWriter"/>.</remarks>
    public string[] LoggedScriptHashes { get; set; } = [];

    /// <summary>
    /// Gets or sets the interval to persist the logged script hashes.
    /// </summary>
    public TimeSpan LoggedScriptHashesPersistInterval { get; set; } = TimeSpan.FromMinutes(5);
}
