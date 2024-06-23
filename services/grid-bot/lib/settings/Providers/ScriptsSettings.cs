namespace Grid.Bot;

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
    public bool LuaVMEnabled => GetOrDefault(
        nameof(LuaVMEnabled),
        true
    );
}
