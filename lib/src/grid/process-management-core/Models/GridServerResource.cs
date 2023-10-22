namespace Grid;

/// <summary>
/// Represents the resources for an Grid Server instance.
/// </summary>
public class GridServerResource
{
    /// <summary>
    /// The cores being used.
    /// </summary>
    public double Cores { get; }

    /// <summary>
    /// The threads being used.
    /// </summary>
    public long Threads { get; }

    /// <summary>
    /// The memory in MiB being used.
    /// </summary>
    public long MemoryMB { get; }

    /// <summary>
    /// Construct a new instance of <see cref="GridServerResource"/>
    /// </summary>
    /// <param name="cores">The cores.</param>
    /// <param name="threads">The threads.</param>
    /// <param name="memoryMB">The memory in MiB</param>
    public GridServerResource(double cores, long threads, long memoryMB)
    {
        Cores = cores;
        Threads = threads;
        MemoryMB = memoryMB;
    }

    /// <inheritdoc cref="System.Object.ToString"/>
    public override string ToString() => $"Cores: {Cores}, Threads: {Threads}, MemoryMB: {MemoryMB}";
}
