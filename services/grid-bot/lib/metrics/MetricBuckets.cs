namespace Grid.Bot.Metrics;

/// <summary>
/// Explicit histogram bucket boundaries, in seconds, shared by the duration metrics.
/// </summary>
public static class MetricBuckets
{
    /// <summary>
    /// Buckets for short operations such as settings refreshes and job acquisition.
    /// </summary>
    public static readonly double[] Fast = [0.001, 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10];

    /// <summary>
    /// Buckets for command and interaction processing.
    /// </summary>
    public static readonly double[] Command = [0.01, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10, 30, 60];

    /// <summary>
    /// Buckets for long-running work such as script executions and renders.
    /// </summary>
    public static readonly double[] Execution = [0.1, 0.25, 0.5, 1, 2.5, 5, 10, 20, 30, 60, 120];
}
