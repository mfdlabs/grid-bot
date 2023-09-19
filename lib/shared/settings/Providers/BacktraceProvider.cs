namespace Grid.Bot;

/// <summary>
/// Settings provider for all Backtrace related stuff.
/// </summary>
public class BacktraceSettings : BaseSettingsProvider<BacktraceSettings>
{
    /// <inheritdoc cref="Configuration.IVaultProvider.Path"/>
    public override string Path => SettingsProvidersDefaults.BacktracePath;

    /// <summary>
    /// Gets the percentage to upload log files to Backtrace.
    /// </summary>
    public int UploadLogFilesToBacktraceEnabledPercent => GetOrDefault(
        nameof(UploadLogFilesToBacktraceEnabledPercent),
        100
    );

    /// <summary>
    /// Gets the url for Backtrace.
    /// </summary>
    public string BacktraceUrl => GetOrDefault(
        nameof(BacktraceUrl),
        "http://mfdlabs.sp.backtrace.io:6097"
    );

    /// <summary>
    /// Gets the backtrace token.
    /// </summary>
    public string BacktraceToken => GetOrDefault(
        nameof(BacktraceToken),
        "9f55e8c1e2a0bc06f874371f33d7c24e38b88c48f3c3cd853fc95174a13beb9b"
    );
}
