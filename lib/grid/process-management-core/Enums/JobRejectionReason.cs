namespace Grid;

/// <summary>
/// Reasoning for a job being rejected.
/// </summary>
public enum JobRejectionReason
{
    /// <summary>
    /// There was no ready instance to take the job.
    /// </summary>
    NoReadyInstance,

    /// <summary>
    /// There was not enough CPU available.
    /// </summary>
    CpuAllocationExceeded,

    /// <summary>
    /// There was not enough CPU threads available.
    /// </summary>
    ThreadsAllocationExceeded,

    /// <summary>
    /// There was not enough memory available.
    /// </summary>
    MemoryAllocationExceeded
}
