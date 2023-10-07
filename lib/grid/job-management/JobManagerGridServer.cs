namespace Grid;

using System;

using Logging;

/// <summary>
/// Implementation for a Grid Server job manager.
/// </summary>
/// <typeparam name="TInstance">The type of instance.</typeparam>
/// <typeparam name="TUnmanagedInstance">The type of unmanaged instances.</typeparam>
public class JobManagerGridServer<TInstance, TUnmanagedInstance>
    where TInstance : IGridServerInstance
    where TUnmanagedInstance : IUnmanagedGridServerInstance
{
    /// <summary>
    /// The logger.
    /// </summary>
    protected ILogger Logger;

    /// <summary>
    /// The job manager.
    /// </summary>
    protected JobManagerBase<TInstance, TUnmanagedInstance> JobManager;

    /// <summary>
    /// Construct a new instance of <see cref="JobManagerGridServer{TInstance, TUnmanagedInstance}"/>
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="jobManager">The <see cref="JobManagerBase{TInstance, TUnmanagedInstance}"/></param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="logger"/> cannot be null.
    /// - <paramref name="jobManager"/> cannot be null.
    /// </exception>
    public JobManagerGridServer(ILogger logger, JobManagerBase<TInstance, TUnmanagedInstance> jobManager)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        JobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
    }

    /// <summary>
    /// Start the job manager.
    /// </summary>
    public void Start() => JobManager.Start();

    /// <summary>
    /// Stop the job manager.
    /// </summary>
    public void Stop() => JobManager.Stop();

    /// <summary>
    /// Get the instance count.
    /// </summary>
    /// <returns>The instance count.</returns>
    public int GetInstanceCount() => JobManager.GetInstanceCount();

    /// <summary>
    /// Get the ready instance count.
    /// </summary>
    /// <returns>The ready instance count.</returns>
    public int GetReadyInstanceCount() => JobManager.GetReadyInstanceCount();

    /// <summary>
    /// Get the active job count.
    /// </summary>
    /// <returns>The active job count.</returns>
    public int GetActiveJobsCount() => JobManager.GetActiveJobsCount();

    /// <summary>
    /// Get the job manager version.
    /// </summary>
    /// <returns>The job manager version.</returns>
    public string GetVersion() => JobManager.GetVersion();

    /// <summary>
    /// Renew the lease of a job.
    /// </summary>
    /// <param name="jobId">The ID of the job.</param>
    /// <param name="expirationInSeconds">The new expiration in seconds.</param>
    /// <returns>The new expiration in seconds.</returns>
    public virtual double RenewLease(string jobId, double expirationInSeconds)
    {
        var job = new Job(jobId);
        Logger.Debug("RenewLease starting. {0}, expirationInSeconds = {1}", job, expirationInSeconds);

        JobManager.RenewLease(job, expirationInSeconds);

        using var soap = JobManager.GetJob(job);
        var newExpiration = soap.RenewLease(jobId, expirationInSeconds);
        Logger.Debug("RenewLease completed. {0}, expirationInSeconds = {1}, returned value = {2}", job, expirationInSeconds, newExpiration);

        return newExpiration;
    }

    /// <summary>
    /// Close an Grid Server job.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    public virtual void CloseJob(string jobId)
    {
        var job = new Job(jobId);
        Logger.Debug("CloseJob starting. {0}", job);

        try
        {
            using var soap = JobManager.GetJob(job);
            soap.CloseJob(jobId);
        }
        finally
        {
            JobManager.CloseJob(job, false);
        }

        Logger.Debug("CloseJob completed. {0}", job);
    }
}
