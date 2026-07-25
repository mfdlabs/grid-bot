namespace Grid;

/// <summary>
/// Represents physical server info.
/// </summary>
public interface IServerInfo
{
    /// <summary>
    /// The logical core count of the server.
    /// </summary>
    int LogicalCoreCount { get; set; }

    /// <summary>
    /// The physical core count of the server.
    /// </summary>
    int PhysicalCoreCount { get; set; }

    /// <summary>
    /// The total physical memory in GiB.
    /// </summary>
    float TotalPhysicalMemoryInGigabytes { get; set; }

    /// <summary>
    /// The assembly version.
    /// </summary>
    string AssemblyVersion { get; set; }

    /// <summary>
    /// The kernel version.
    /// </summary>
    string KernelVersion { get; set; }
}
