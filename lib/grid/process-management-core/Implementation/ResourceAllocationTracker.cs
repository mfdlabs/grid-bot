namespace Grid;

using System;

using Newtonsoft.Json;

/// <summary>
/// Represents the class for tracking resource allocation.
/// </summary>
public class ResourceAllocationTracker
{
    private static readonly double _DoubleValuesEqualityThreshold = 1E-05;
    private readonly object _ResourceAllocationLock = new();

    /// <summary>
    /// Is CPU allocation check enabled?
    /// </summary>
    public readonly Func<bool> IsCpuAllocationCheckEnabled;

    /// <summary>
    /// Is thread allocation check enabled?
    /// </summary>
    public readonly Func<bool> IsThreadsAllocationCheckEnabled;

    /// <summary>
    /// Is memory allocation check enabled?
    /// </summary>
    public readonly Func<bool> IsMemoryAllocationCheckEnabled;

    /// <summary>
    /// CPU over-allocation ratio.
    /// </summary>
    public readonly Func<double> CpuOverAllocationRatio;

    /// <summary>
    /// Thread over-allocation ratio.
    /// </summary>
    public readonly Func<double> ThreadsOverAllocationRatio;

    /// <summary>
    /// Memory over-allocation ratio.
    /// </summary>
    public readonly Func<double> MemoryOverAllocationRatio;

    /// <summary>
    /// Total physical cores.
    /// </summary>
    public readonly long TotalPhysicalCores;

    /// <summary>
    /// Total threads allowed.
    /// </summary>
    public readonly long TotalThreadsAllowed;

    /// <summary>
    /// Total memory in MiB.
    /// </summary>
    public readonly long TotalMemoryInMegabytes;

    /// <summary>
    /// The allocated physical machine cores.
    /// </summary>
    public double AllocatedPhysicalCores { get; private set; }

    /// <summary>
    /// The allocated memory in MiB.
    /// </summary>
    public long AllocatedMemoryInMegabytes { get; private set; }

    /// <summary>
    /// The allocated threads.
    /// </summary>
    public long AllocatedThreads { get; private set; }

    /// <summary>
    /// Constructs a new instance of <see cref="ResourceAllocationTracker"/>
    /// </summary>
    public ResourceAllocationTracker()
        : this(0, 0, 0, () => false, () => false, () => false, () => 1, () => 1, () => 1)
    {
    }

    /// <summary>
    /// Constructs a new instance of <see cref="ResourceAllocationTracker"/>
    /// </summary>
    /// <param name="totalPhysicalCores">The total physical cores.</param>
    /// <param name="totalThreadsAllowed">The total threads allowed.</param>
    /// <param name="totalMemoryInMegabytes">The toal memory in MiB.</param>
    /// <param name="isCpuAllocationCheckEnabled">Is the CPU allocation check enabled?</param>
    /// <param name="isThreadsAllocationCheckEnabled">Is the threads allocation check enabled?</param>
    /// <param name="isMemoryAllocationCheckEnabled">Is the memory allocation check enabkled?</param>
    /// <param name="cpuOverAllocationRatio">The CPU over-allocation ratio.</param>
    /// <param name="threadsOverAllocationRatio">The threads over-allocation ratio.</param>
    /// <param name="memoryOverAllocationRatio">The memory over-allocation ratio.</param>
    public ResourceAllocationTracker(
        long totalPhysicalCores,
        long totalThreadsAllowed,
        long totalMemoryInMegabytes,
        Func<bool> isCpuAllocationCheckEnabled,
        Func<bool> isThreadsAllocationCheckEnabled,
        Func<bool> isMemoryAllocationCheckEnabled,
        Func<double> cpuOverAllocationRatio,
        Func<double> threadsOverAllocationRatio,
        Func<double> memoryOverAllocationRatio
    )
    {
        TotalPhysicalCores = totalPhysicalCores;
        TotalThreadsAllowed = totalThreadsAllowed;
        TotalMemoryInMegabytes = totalMemoryInMegabytes;
        IsCpuAllocationCheckEnabled = isCpuAllocationCheckEnabled ?? (() => false);
        IsThreadsAllocationCheckEnabled = isThreadsAllocationCheckEnabled ?? (() => false);
        IsMemoryAllocationCheckEnabled = isMemoryAllocationCheckEnabled ?? (() => false);
        CpuOverAllocationRatio = cpuOverAllocationRatio ?? (() => 1);
        ThreadsOverAllocationRatio = threadsOverAllocationRatio ?? (() => 1);
        MemoryOverAllocationRatio = memoryOverAllocationRatio ?? (() => 1);
    }

    /// <summary>
    /// Is resource allocation check enabled?
    /// </summary>
    /// <returns>True if resource allocation check is enabled.</returns>
    public bool IsResourceAllocationCheckEnabled() => IsCpuAllocationCheckEnabled() || IsThreadsAllocationCheckEnabled() || IsMemoryAllocationCheckEnabled();

    /// <summary>
    /// Gets the status of a resource.
    /// </summary>
    /// <param name="resourceNeeded">The resource needed.</param>
    /// <returns>A tuple of success and job rejection reason.</returns>
    public (bool success, JobRejectionReason? rejectionReason) IsResourceAvailable(GridServerResource resourceNeeded)
    {
        lock (_ResourceAllocationLock)
        {
            if (!IsCpuAvailable(resourceNeeded.Cores))
                return (false, JobRejectionReason.CpuAllocationExceeded);
            else if (!IsThreadsAvailable(resourceNeeded.Threads))
                return (false, JobRejectionReason.ThreadsAllocationExceeded);
            else if (!IsMemoryAvailable(resourceNeeded.MemoryMB))
                return (false, JobRejectionReason.MemoryAllocationExceeded);

            return (true, null);
        }
    }

    /// <summary>
    /// Update the resource allocation with new resources.
    /// </summary>
    /// <param name="resource">The new resource.</param>
    public void UpdateResourceAllocation(GridServerResource resource)
    {
        lock (_ResourceAllocationLock)
        {
            AllocatedPhysicalCores = resource.Cores;
            AllocatedThreads = resource.Threads;
            AllocatedMemoryInMegabytes = resource.MemoryMB;
        }
    }

    /// <summary>
    /// Reset the resource allocation.
    /// </summary>
    public void ResetResourceAllocation() => UpdateResourceAllocation(new(0, 0, 0));

    /// <summary>
    /// Conver to JSON string.
    /// </summary>
    /// <returns>The JSON string.</returns>
    public string ToJsonString()
    {
        return JsonConvert.SerializeObject(
            new
            {
                IsCpuAllocationCheckEnabled = IsCpuAllocationCheckEnabled(),
                IsThreadsAllocationCheckEnabled = IsThreadsAllocationCheckEnabled(),
                IsMemoryAllocationCheckEnabled = IsMemoryAllocationCheckEnabled(),
                CpuOverAllocationRatio = CpuOverAllocationRatio(),
                ThreadsOverAllocationRatio = ThreadsOverAllocationRatio(),
                MemoryOverAllocationRatio = MemoryOverAllocationRatio(),
                TotalPhysicalCores,
                AllocatedPhysicalCores,
                TotalThreadsAllowed,
                AllocatedThreads,
                TotalMemoryInMegabytes,
                AllocatedMemoryInMegabytes
            },
            Formatting.Indented
        );
    }

    private bool IsCpuAvailable(double physicalCoresNeeded)
        =>
        physicalCoresNeeded <= 0 ||
        !IsCpuAllocationCheckEnabled() ||
        AllocatedPhysicalCores + physicalCoresNeeded <
        TotalPhysicalCores * CpuOverAllocationRatio() + _DoubleValuesEqualityThreshold;

    private bool IsThreadsAvailable(long threadsNeeded)
        =>
        threadsNeeded <= 0 ||
        !IsThreadsAllocationCheckEnabled() ||
        (AllocatedThreads + threadsNeeded) <
        TotalThreadsAllowed * ThreadsOverAllocationRatio() + _DoubleValuesEqualityThreshold;

    private bool IsMemoryAvailable(long memoryInMegabytesNeeded)
        =>
        memoryInMegabytesNeeded <= 0 ||
        !IsMemoryAllocationCheckEnabled() ||
        (AllocatedMemoryInMegabytes + memoryInMegabytesNeeded) <
        TotalMemoryInMegabytes * MemoryOverAllocationRatio() + _DoubleValuesEqualityThreshold;


}
