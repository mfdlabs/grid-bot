namespace Grid.Bot.Utility;

using System;
using System.Collections.Generic;

using Client;

#if DEBUG

/// <summary>
/// Job manager implementation that does nothing, used for debugging.
/// </summary>
public class NoopJobManager : IJobManager
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static NoopJobManager Singleton { get; } = new();

    /// <inheritdoc cref="IJobManager.GetInstanceCount"/>
    public int GetInstanceCount() => 0;

    /// <inheritdoc cref="IJobManager.GetReadyInstanceCount"/>
    public int GetReadyInstanceCount() => 0;

    /// <inheritdoc cref="IJobManager.GetActiveJobsCount"/>
    public int GetActiveJobsCount() => 0;

    /// <inheritdoc cref="IJobManager.GetAllRunningJobIds"/>
    public IReadOnlyCollection<string> GetAllRunningJobIds() => [];

    /// <inheritdoc cref="IJobManager.AddOrUpdateActiveJob"/>
    public void AddOrUpdateActiveJob(IJob job, IGridServerInstance instance) {}

    /// <inheritdoc cref="IJobManager.IsResourceAvailable"/>
    public (bool isAvailable, JobRejectionReason? rejectionReason) IsResourceAvailable(GridServerResource resourceNeeded) 
        => (false, JobRejectionReason.NoReadyInstance);

    /// <inheritdoc cref="IJobManager.GetAllocatedResource"/>
    public GridServerResource GetAllocatedResource() => null;

    /// <inheritdoc cref="IJobManager.RenewLease(IJob, double)"/>
    public void RenewLease(IJob job, double leaseTimeInSeconds) {}

    /// <inheritdoc cref="IJobManager.NewJob(IJob, double, bool, bool)"/>
    public (GridServerServiceSoap soapInterface, IGridServerInstance instance, JobRejectionReason? rejectionReason) NewJob(
        IJob job,
        double expirationInSeconds,
        bool waitForReadyInstance = false,
        bool addToActiveJobs = true
    ) => (null, null, JobRejectionReason.NoReadyInstance);

    /// <inheritdoc cref="IJobManager.GetJob(IJob)"/>
    public GridServerServiceSoap GetJob(IJob job) => null;

    /// <inheritdoc cref="IJobManager.CloseJob(IJob, bool)"/>
    public void CloseJob(IJob job, bool removeFromActiveJobs = true) {}

    /// <inheritdoc cref="IJobManager.GetVersion"/>
    public string GetVersion() => "noop";

    /// <inheritdoc cref="IJobManager.GetUnexpectedExitGameJobs"/>
    public IReadOnlyCollection<GameJob> GetUnexpectedExitGameJobs() => [];

    /// <inheritdoc cref="IJobManager.DispatchRequestToAllActiveJobs(Action{GridServerServiceSoap})"/>
    public void DispatchRequestToAllActiveJobs(Action<GridServerServiceSoap> action) {}

    /// <inheritdoc cref="IJobManager.GetGridServerInstanceId(string)"/>
    public string GetGridServerInstanceId(string jobId) => jobId;

    /// <inheritdoc cref="IJobManager.UpdateGridServerInstance(GridServerResourceJob)"/>
    public bool UpdateGridServerInstance(GridServerResourceJob job) => false;
}

#endif

/// <summary>
/// A class that manages the jobs for the bot.
/// </summary>
public class JobManager : IJobManager
{
    private readonly DockerJobManager _dockerJobManager;
    private readonly ProcessJobManager _processJobManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobManager"/> class.
    /// </summary>
    /// <param name="dockerJobManager">The docker job manager.</param>
    /// <param name="processJobManager">The process job manager.</param>    
    /// <exception cref="ArgumentNullException">If both <paramref name="dockerJobManager"/> and <paramref name="processJobManager"/> are null.</exception>
    public JobManager(DockerJobManager dockerJobManager, ProcessJobManager processJobManager)
    {
        if (dockerJobManager == null && processJobManager == null)
            throw new ArgumentNullException(nameof(dockerJobManager), "Both dockerJobManager and processJobManager cannot be null.");

        _dockerJobManager = dockerJobManager;
        _processJobManager = processJobManager;
    }

    private JobManagerBase GetJobManager()
    {
        if (_dockerJobManager != null)
            return _dockerJobManager;

        return _processJobManager;
    }

    /// <inheritdoc cref="IJobManager.GetInstanceCount"/>
    public int GetInstanceCount() => GetJobManager().GetInstanceCount();

    /// <inheritdoc cref="IJobManager.GetReadyInstanceCount"/>
    public int GetReadyInstanceCount() => GetJobManager().GetReadyInstanceCount();

    /// <inheritdoc cref="IJobManager.GetActiveJobsCount"/>
    public int GetActiveJobsCount() => GetJobManager().GetActiveJobsCount();

    /// <inheritdoc cref="IJobManager.GetAllRunningJobIds"/>
    public IReadOnlyCollection<string> GetAllRunningJobIds() => GetJobManager().GetAllRunningJobIds();

    /// <inheritdoc cref="IJobManager.AddOrUpdateActiveJob"/>
    public void AddOrUpdateActiveJob(IJob job, IGridServerInstance instance) => GetJobManager().AddOrUpdateActiveJob(job, instance);

    /// <inheritdoc cref="IJobManager.IsResourceAvailable"/>
    public (bool isAvailable, JobRejectionReason? rejectionReason) IsResourceAvailable(GridServerResource resourceNeeded) => GetJobManager().IsResourceAvailable(resourceNeeded);

    /// <inheritdoc cref="IJobManager.GetAllocatedResource"/>
    public GridServerResource GetAllocatedResource() => GetJobManager().GetAllocatedResource();

    /// <inheritdoc cref="IJobManager.RenewLease(IJob, double)"/>
    public void RenewLease(IJob job, double leaseTimeInSeconds) => GetJobManager().RenewLease(job, leaseTimeInSeconds);

    /// <inheritdoc cref="IJobManager.NewJob(IJob, double, bool, bool)"/>
    public (GridServerServiceSoap soapInterface, IGridServerInstance instance, JobRejectionReason? rejectionReason) NewJob(
        IJob job,
        double expirationInSeconds,
        bool waitForReadyInstance = false,
        bool addToActiveJobs = true
    ) => GetJobManager().NewJob(job, expirationInSeconds, waitForReadyInstance, addToActiveJobs);

    /// <inheritdoc cref="IJobManager.GetJob(IJob)"/>
    public GridServerServiceSoap GetJob(IJob job) => GetJobManager().GetJob(job);

    /// <inheritdoc cref="IJobManager.CloseJob(IJob, bool)"/>
    public void CloseJob(IJob job, bool removeFromActiveJobs = true) => GetJobManager().CloseJob(job, removeFromActiveJobs);

    /// <inheritdoc cref="IJobManager.GetVersion"/>
    public string GetVersion() => GetJobManager().GetVersion();

    /// <inheritdoc cref="IJobManager.GetUnexpectedExitGameJobs"/>
    public IReadOnlyCollection<GameJob> GetUnexpectedExitGameJobs() => GetJobManager().GetUnexpectedExitGameJobs();

    /// <inheritdoc cref="IJobManager.DispatchRequestToAllActiveJobs(Action{GridServerServiceSoap})"/>
    public void DispatchRequestToAllActiveJobs(Action<GridServerServiceSoap> action) => GetJobManager().DispatchRequestToAllActiveJobs(action);

    /// <inheritdoc cref="IJobManager.GetGridServerInstanceId(string)"/>
    public string GetGridServerInstanceId(string jobId) => GetJobManager().GetGridServerInstanceId(jobId);

    /// <inheritdoc cref="IJobManager.UpdateGridServerInstance(GridServerResourceJob)"/>
    public bool UpdateGridServerInstance(GridServerResourceJob job) => GetJobManager().UpdateGridServerInstance(job);
}
