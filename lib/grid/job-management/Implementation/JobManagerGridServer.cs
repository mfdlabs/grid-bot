namespace Grid.JobManagement;

using System;
using System.Collections.Generic;

using Logging;

using Grid;
using Grid.Client;
using Grid.Commands;
using ProcessManagement.Core;

using GridJob = Grid.Client.Job;
using Job = ProcessManagement.Core.Job;

/// <summary>
/// Implementation for a Grid Server job manager.
/// </summary>
internal class JobManagerGridServer : IJobManagerGridServer
{
    /// <summary>
    /// The logger.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// The job manager.
    /// </summary>
    public JobManagerBase JobManager { get; private set; }

    /// <summary>
    /// Construct a new instance of <see cref="JobManagerGridServer"/>
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="jobManager">The <see cref="JobManagerBase"/></param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="logger"/> cannot be null.
    /// - <paramref name="jobManager"/> cannot be null.
    /// </exception>
    public JobManagerGridServer(ILogger logger, JobManagerBase jobManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        JobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
    }

    /// <inheritdoc cref="IJobManagerGridServer.Start"/>
    public void Start() => JobManager.Start();

    /// <inheritdoc cref="IJobManagerGridServer.Stop"/>
    public void Stop() => JobManager.Stop();

    /// <inheritdoc cref="IJobManagerGridServer.GetInstanceCount"/>
    public int GetInstanceCount() => JobManager.GetInstanceCount();

    /// <inheritdoc cref="IJobManagerGridServer.GetReadyInstanceCount"/>
    public int GetReadyInstanceCount() => JobManager.GetReadyInstanceCount();

    /// <inheritdoc cref="IJobManagerGridServer.GetActiveJobsCount"/>
    public int GetActiveJobsCount() => JobManager.GetActiveJobsCount();

    /// <inheritdoc cref="IJobManagerGridServer.GetVersion"/>
    public string GetVersion() => JobManager.GetVersion();

    /// <inheritdoc cref="IJobManagerGridServer.GetAllRunningJobIds"/>
    public IReadOnlyCollection<string> GetAllRunningJobIds() => JobManager.GetAllRunningJobIds();

    /// <inheritdoc cref="IJobManagerGridServer.AddOrUpdateActiveJob"/>
    public void AddOrUpdateActiveJob(IJob job, IGridServerInstance instance) => JobManager.AddOrUpdateActiveJob(job, instance);

    /// <inheritdoc cref="IJobManagerGridServer.IsResourceAvailable"/>
    public (bool isAvailable, JobRejectionReason? rejectionReason) IsResourceAvailable(GridServerResource resourceNeeded) => JobManager.IsResourceAvailable(resourceNeeded);

    /// <inheritdoc cref="IJobManagerGridServer.GetAllocatedResource"/>
    public GridServerResource GetAllocatedResource() => JobManager.GetAllocatedResource();

    /// <inheritdoc cref="IJobManagerGridServer.RenewLease(IJob, double)"/>
    public void RenewLease(IJob job, double leaseTimeInSeconds) => JobManager.RenewLease(job, leaseTimeInSeconds);

    /// <inheritdoc cref="IJobManagerGridServer.NewJob(IJob, double, bool, bool)"/>
    public (GridServerServiceSoap soapInterface, IGridServerInstance instance, JobRejectionReason? rejectionReason) NewJob(
        IJob job,
        double expirationInSeconds,
        bool waitForReadyInstance = false,
        bool addToActiveJobs = true
    ) => JobManager.NewJob(job, expirationInSeconds, waitForReadyInstance, addToActiveJobs);

    /// <inheritdoc cref="IJobManagerGridServer.GetJob(IJob)"/>
    public GridServerServiceSoap GetJob(IJob job) => JobManager.GetJob(job);

    /// <inheritdoc cref="IJobManagerGridServer.CloseJob(IJob, bool)"/>
    public void CloseJob(IJob job, bool removeFromActiveJobs = true) => JobManager.CloseJob(job, removeFromActiveJobs);

    /// <inheritdoc cref="IJobManagerGridServer.GetUnexpectedExitGameJobs"/>
    public IReadOnlyCollection<GameJob> GetUnexpectedExitGameJobs() => JobManager.GetUnexpectedExitGameJobs();

    /// <inheritdoc cref="IJobManagerGridServer.DispatchRequestToAllActiveJobs(Action{GridServerServiceSoap})"/>
    public void DispatchRequestToAllActiveJobs(Action<GridServerServiceSoap> action) => JobManager.DispatchRequestToAllActiveJobs(action);

    /// <inheritdoc cref="IJobManagerGridServer.GetGridServerInstanceId(string)"/>
    public string GetGridServerInstanceId(string jobId) => JobManager.GetGridServerInstanceId(jobId);

    /// <inheritdoc cref="IJobManagerGridServer.UpdateGridServerInstance(GridServerResourceJob)"/>
    public bool UpdateGridServerInstance(GridServerResourceJob job) => JobManager.UpdateGridServerInstance(job);

    /// <inheritdoc cref="IJobManagerGridServer.RenewLease(string, double)"/>
    public virtual double RenewLease(string jobId, double expirationInSeconds)
    {
        var job = new Job(jobId);
        _logger.Information("RenewLease starting. {0}, expirationInSeconds = {1}", job, expirationInSeconds);

        JobManager.RenewLease(job, expirationInSeconds);

        using var soap = JobManager.GetJob(job);
        var newExpiration = soap.RenewLease(jobId, expirationInSeconds);
        _logger.Information("RenewLease completed. {0}, expirationInSeconds = {1}, returned value = {2}", job, expirationInSeconds, newExpiration);

        return newExpiration;
    }

    /// <inheritdoc cref="IJobManagerGridServer.CloseJob(string)"/>
    public virtual void CloseJob(string jobId)
    {
        var job = new Job(jobId);
        _logger.Information("CloseJob starting. {0}", job);

        try
        {
            using var soap = JobManager.GetJob(job);
            soap.CloseJob(jobId);
        }
        finally
        {
            JobManager.CloseJob(job, false);
        }

        _logger.Information("CloseJob completed. {0}", job);
    }

    /// <inheritdoc cref="IJobManagerGridServer.RunBatchJob(GridJob, ScriptExecution)"/>
    public LuaValue[] RunBatchJob(GridJob gridJob, ScriptExecution script)
    {
        _logger.Information("RunBatchJob starting. Job ID = {0}", gridJob.id);

        var job = new Job(gridJob.id);

        try
        {
            var (soap, _, rejectionReason) = JobManager.NewJob(job, gridJob.expirationInSeconds, true);
            if (rejectionReason != null)
                throw new Exception($"JobManager.NewJob was rejected. Rejection Reason: {rejectionReason.Value}");

            using (soap)
            {
                var data = soap.BatchJobEx(gridJob, script);

                JobManager.CloseJob(job, true);

                _logger.Information("RunBatchJob completed. Job ID = {0}, Category = {1}, ExpirationInSeconds = {2}", gridJob.id, gridJob.category, gridJob.expirationInSeconds);

                return data;
            }
        }
        catch (Exception ex)
        {
            JobManager.CloseJob(job, false);

            _logger.Error(
                "RunJob failed. Job ID = {0}, Category = {1}, ExpirationInSeconds = {2}, Exception = {3}",
                gridJob.id,
                gridJob.category,
                gridJob.expirationInSeconds,
                ex
            );

            throw;
        }
    }

    /// <inheritdoc cref="IJobManagerGridServer.RunBatchJob(GridJob, GridCommand)"/>
    public LuaValue[] RunBatchJob(GridJob gridJob, GridCommand script)
    {
        _logger.Information("RunBatchJob starting. Job ID = {0}", gridJob.id);

        var job = new Job(gridJob.id);

        try
        {
            var (soap, _, rejectionReason) = JobManager.NewJob(job, gridJob.expirationInSeconds, true);
            if (rejectionReason != null)
                throw new Exception($"JobManager.NewJob was rejected. Rejection Reason: {rejectionReason.Value}");

            using (soap)
            {
                var data = soap.BatchJobEx(gridJob, script);

                JobManager.CloseJob(job, true);

                _logger.Information("RunBatchJob completed. Job ID = {0}, Category = {1}, ExpirationInSeconds = {2}", gridJob.id, gridJob.category, gridJob.expirationInSeconds);

                return data;
            }
        }
        catch (Exception ex)
        {
            JobManager.CloseJob(job, false);

            _logger.Error(
                "RunJob failed. Job ID = {0}, Category = {1}, ExpirationInSeconds = {2}, Exception = {3}",
                gridJob.id,
                gridJob.category,
                gridJob.expirationInSeconds,
                ex
            );

            throw;
        }
    }
}
