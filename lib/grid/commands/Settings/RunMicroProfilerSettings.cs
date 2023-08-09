namespace Grid.Commands;

/// <summary>
/// Settings for the <see cref="RunMicroProfilerCommand"/>
/// </summary>
public class RunMicroProfilerSettings
{
    /// <summary>
    /// The seconds to record
    /// </summary>
    public double SecondsToRecord { get; }

    /// <summary>
    /// The output file name.
    /// </summary>
    public string OutputFileName { get; }

    /// <summary>
    /// Construct a new instance of <see cref="RunMicroProfilerSettings"/>
    /// </summary>
    /// <param name="secondsToRecord">The seconds to record.</param>
    /// <param name="outputFileName">The output file name.</param>
    public RunMicroProfilerSettings(double secondsToRecord, string outputFileName)
    {
        SecondsToRecord = secondsToRecord;
        OutputFileName = outputFileName;
    }
}
