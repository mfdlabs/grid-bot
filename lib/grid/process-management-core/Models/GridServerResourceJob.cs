namespace Grid;

/// <summary>
/// Represents a job to discover the Grid Server resources.
/// </summary>
public class GridServerResourceJob : Job
{
    /// <summary>
    /// The ID of the container.
    /// </summary>
    public string ContainerId { get; }

    /// <summary>
    /// The ID of the game.
    /// </summary>
    public string GameId { get; }

    /// <summary>
    /// The period of the CPU scheduler job.
    /// </summary>
    public long SchedulerCpuPeriod { get; }

    /// <summary>
    /// The max cores.
    /// </summary>
    public double MaximumCores { get; }

    /// <summary>
    /// The max threads.
    /// </summary>
    public long MaximumThreads { get; }

    /// <summary>
    /// The max memory in MiB.
    /// </summary>
    public long MaximumMemoryInMegabytes { get; }

    /// <summary>
    /// Construct a new instance of <see cref="GridServerResourceJob"/>
    /// </summary>
    /// <param name="containerId">The container ID.</param>
    /// <param name="gameId">The game ID.</param>
    /// <param name="schedulerCpuPeriod">The period of the CPU scheduler job.</param>
    /// <param name="maximumCores">The max cores.</param>
    /// <param name="maximumThreads">The max threads.</param>
    /// <param name="maximumMemoryInMegabytes">The max memory in MiB.</param>
    public GridServerResourceJob(
        string containerId,
        string gameId, 
        long schedulerCpuPeriod,
        double maximumCores, 
        long maximumThreads,
        long maximumMemoryInMegabytes
    ) 
        : base(gameId)
    {
        ContainerId = containerId;
        GameId = gameId;
        SchedulerCpuPeriod = schedulerCpuPeriod;
        MaximumCores = maximumCores;
        MaximumThreads = maximumThreads;
        MaximumMemoryInMegabytes = maximumMemoryInMegabytes;
    }

    /// <inheritdoc cref="System.Object.ToString"/>
    public override string ToString()
        => 
        $"{base.ToString()}," +
        $" ContainerId = {ContainerId}," +
        $" GameId = {GameId}," +
        $" SchedulerCpuPeriod = {SchedulerCpuPeriod}," +
        $" MaximumCores = {MaximumCores}," +
        $" MaximumThreads = {MaximumThreads}," +
        $" MaximumMemoryInMegabytes = {MaximumMemoryInMegabytes}";
}
