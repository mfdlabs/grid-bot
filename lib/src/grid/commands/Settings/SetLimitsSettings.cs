namespace Grid.Commands;

/// <summary>
/// The settings for <see cref="SetLimitsCommand"/>
/// </summary>
public class SetLimitsSettings
{
    /// <summary>
    /// The maximum cores.
    /// </summary>
    public double MaximumCores { get; }

    /// <summary>
    /// The maximum threads.
    /// </summary>
    public long MaximumThreads { get; }

    /// <summary>
    /// The maximum memory in MiB.
    /// </summary>
    public long MaximumMemoryMB { get; }

    /// <summary>
    /// The period for the CPU scheduler.
    /// </summary>
    public long SchedulerCpuPeriod { get; }

    /// <summary>
    /// Construct a new instance of <see cref="SetLimitsSettings"/>
    /// </summary>
    /// <param name="maximumCores">The maximum cores.</param>
    /// <param name="maximumThreads">The maximum threads.</param>
    /// <param name="maximumMemoryMB">The maximum memory in MiB.</param>
    /// <param name="schedulerCpuPeriod">The period for the CPU scheduler.</param>
    public SetLimitsSettings(double maximumCores, long maximumThreads, long maximumMemoryMB, long schedulerCpuPeriod)
    {
        MaximumCores = maximumCores;
        MaximumThreads = maximumThreads;
        MaximumMemoryMB = maximumMemoryMB;
        SchedulerCpuPeriod = schedulerCpuPeriod;
    }
}
