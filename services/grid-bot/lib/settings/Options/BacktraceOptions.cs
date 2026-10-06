namespace Grid.Bot;

/// <summary>
/// Options for Backtrace.
/// </summary>
public class BacktraceOptions
{
    /// <summary>
    /// The configuration section these options bind from.
    /// </summary>
    public const string SectionName = "Backtrace";

    /// <summary>
    /// The vault path for the Backtrace options.
    /// </summary>
    public const string VaultPath = "backtrace";

    /// <summary>
    /// Gets or sets the percentage to upload log files to Backtrace.
    /// </summary>
    public int UploadLogFilesToBacktraceEnabledPercent { get; set; } = 100;

    /// <summary>
    /// Gets or sets the url for Backtrace.
    /// </summary>
    public string BacktraceUrl { get; set; } = "http://mfdlabs.sp.backtrace.io:6097";

    /// <summary>
    /// Gets or sets the Backtrace token.
    /// </summary>
    public string BacktraceToken { get; set; } = "9f55e8c1e2a0bc06f874371f33d7c24e38b88c48f3c3cd853fc95174a13beb9b";
}
