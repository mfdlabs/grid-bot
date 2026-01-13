namespace Grid.Bot;

using System;

/// <summary>
/// Settings provider for all script execution related stuff.
/// </summary>
public class ScriptsSettings : BaseSettingsProvider
{
    /// <inheritdoc cref="Configuration.IVaultProvider.Path"/>
    public override string Path => SettingsProvidersDefaults.ScriptsPath;

    /// <summary>
    /// Gets the max size for a script used by the Execute Script commands in KiB.
    /// </summary>
    public int ScriptExecutionMaxFileSizeKb => GetOrDefault(
        nameof(ScriptExecutionMaxFileSizeKb),
        75
    );

    /// <summary>
    /// Gets the max size for the result file used by the Execute Script command in KiB.
    /// </summary>
    public int ScriptExecutionMaxResultSizeKb => GetOrDefault(
        nameof(ScriptExecutionMaxResultSizeKb),
        50
    );

    /// <summary>
    /// Determines if the LuaVM is enabled.
    /// </summary>
    public bool LuaVmEnabled => GetOrDefault(
        nameof(LuaVmEnabled),
        true
    );

    /// <summary>
    /// Gets the percentage to use for logging scripts.
    /// </summary>
    public int ScriptLoggingPercentage => GetOrDefault(
        nameof(ScriptLoggingPercentage),
        0
    );

    /// <summary>
    /// Gets a Discord webhook URL to send script logs to.
    /// </summary>
    public string ScriptLoggingDiscordWebhookUrl => GetOrDefault(
        nameof(ScriptLoggingDiscordWebhookUrl),
        string.Empty
    );

    /// <summary>
    /// Gets or sets a list of hashes of scripts that have already been logged and should not be logged again.
    /// </summary>
    public string[] LoggedScriptHashes
    {
        get => GetOrDefault(
            nameof(LoggedScriptHashes),
            Array.Empty<string>()
        );
        set => Set(nameof(LoggedScriptHashes), value);
    }

    /// <summary>
    /// Gets the interval to persist the logged script hashes.
    /// </summary>
    public TimeSpan LoggedScriptHashesPersistInterval => GetOrDefault(
        nameof(LoggedScriptHashesPersistInterval),
        TimeSpan.FromMinutes(5)
    );
}
