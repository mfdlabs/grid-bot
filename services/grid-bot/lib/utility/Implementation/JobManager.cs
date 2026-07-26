namespace Grid.Bot.Utility;

using System;
using System.Collections.Generic;

using Client;

using JobManagement;
using ProcessManagement.Core;

#if DEBUG

/// <summary>
/// Job manager implementation that does nothing, used for debugging.
/// </summary>
public class NoopJobManager : IJobManager
{
    private static readonly NoopJobManager _singleton = new();

    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static NoopJobManager Singleton => _singleton;

    /// <inheritdoc cref="IJobManager.GetInstanceCount"/>
    public int GetInstanceCount() => default;

    /// <inheritdoc cref="IJobManager.GetReadyInstanceCount"/>
    public int GetReadyInstanceCount() => default;

    /// <inheritdoc cref="IJobManager.GetActiveJobsCount"/>
    public int GetActiveJobsCount() => default;

    /// <inheritdoc cref="IJobManager.GetAllRunningJobIds"/>
    public IReadOnlyCollection<string> GetAllRunningJobIds() => default;

    /// <inheritdoc cref="IJobManager.AddOrUpdateActiveJob"/>
    public void AddOrUpdateActiveJob(IJob job, IGridServerInstance instance) {}

    /// <inheritdoc cref="IJobManager.IsResourceAvailable"/>
    public (bool isAvailable, JobRejectionReason? rejectionReason) IsResourceAvailable(GridServerResource resourceNeeded) => (false, JobRejectionReason.NoReadyInstance);

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
    private readonly IJobManagerGridServer _jobManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobManager"/> class.
    /// </summary>
    /// <param name="jobManager">The <see cref="IJobManagerGridServer"/></param>
    /// <exception cref="ArgumentNullException"><paramref name="jobManager"/> cannot be null.</exception>
    public JobManager(IJobManagerGridServer jobManager)
    {
        _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
    }

    /// <inheritdoc cref="IJobManager.GetInstanceCount"/>
    public int GetInstanceCount() => _jobManager.GetInstanceCount();

    /// <inheritdoc cref="IJobManager.GetReadyInstanceCount"/>
    public int GetReadyInstanceCount() => _jobManager.GetReadyInstanceCount();

    /// <inheritdoc cref="IJobManager.GetActiveJobsCount"/>
    public int GetActiveJobsCount() => _jobManager.GetActiveJobsCount();

    /// <inheritdoc cref="IJobManager.GetAllRunningJobIds"/>
    public IReadOnlyCollection<string> GetAllRunningJobIds() => _jobManager.GetAllRunningJobIds();

    /// <inheritdoc cref="IJobManager.AddOrUpdateActiveJob"/>
    public void AddOrUpdateActiveJob(IJob job, IGridServerInstance instance) => _jobManager.AddOrUpdateActiveJob(job, instance);

    /// <inheritdoc cref="IJobManager.IsResourceAvailable"/>
    public (bool isAvailable, JobRejectionReason? rejectionReason) IsResourceAvailable(GridServerResource resourceNeeded) => _jobManager.IsResourceAvailable(resourceNeeded);

    /// <inheritdoc cref="IJobManager.GetAllocatedResource"/>
    public GridServerResource GetAllocatedResource() => _jobManager.GetAllocatedResource();

    /// <inheritdoc cref="IJobManager.RenewLease(IJob, double)"/>
    public void RenewLease(IJob job, double leaseTimeInSeconds) => _jobManager.RenewLease(job, leaseTimeInSeconds);

    /// <inheritdoc cref="IJobManager.NewJob(IJob, double, bool, bool)"/>
    public (GridServerServiceSoap soapInterface, IGridServerInstance instance, JobRejectionReason? rejectionReason) NewJob(
        IJob job,
        double expirationInSeconds,
        bool waitForReadyInstance = false,
        bool addToActiveJobs = true
    ) => _jobManager.NewJob(job, expirationInSeconds, waitForReadyInstance, addToActiveJobs);

    /// <inheritdoc cref="IJobManager.GetJob(IJob)"/>
    public GridServerServiceSoap GetJob(IJob job) => _jobManager.GetJob(job);

    /// <inheritdoc cref="IJobManager.CloseJob(IJob, bool)"/>
    public void CloseJob(IJob job, bool removeFromActiveJobs = true) => _jobManager.CloseJob(job, removeFromActiveJobs);

    /// <inheritdoc cref="IJobManager.GetVersion"/>
    public string GetVersion() => _jobManager.GetVersion();

    /// <inheritdoc cref="IJobManager.GetUnexpectedExitGameJobs"/>
    public IReadOnlyCollection<GameJob> GetUnexpectedExitGameJobs() => _jobManager.GetUnexpectedExitGameJobs();

    /// <inheritdoc cref="IJobManager.DispatchRequestToAllActiveJobs(Action{GridServerServiceSoap})"/>
    public void DispatchRequestToAllActiveJobs(Action<GridServerServiceSoap> action) => _jobManager.DispatchRequestToAllActiveJobs(action);

    /// <inheritdoc cref="IJobManager.GetGridServerInstanceId(string)"/>
    public string GetGridServerInstanceId(string jobId) => _jobManager.GetGridServerInstanceId(jobId);

    /// <inheritdoc cref="IJobManager.UpdateGridServerInstance(GridServerResourceJob)"/>
    public bool UpdateGridServerInstance(GridServerResourceJob job) => _jobManager.UpdateGridServerInstance(job);
}
