namespace Grid.Bot.Utility;

using System;
using System.Collections.Generic;

using ComputeCloud;

/// <summary>
/// Interface for a job manager.
/// </summary>
public interface IJobManager
{
    /// <summary>
    /// Get the current amount of instances.
    /// </summary>
    /// <returns>The count of instances.</returns>
    int GetInstanceCount();

    /// <summary>
    /// Get the current amount of ready instances.
    /// </summary>
    /// <returns>The count of ready instances.</returns>
    int GetReadyInstanceCount();

    /// <summary>
    /// Get the current amount of active jobs.
    /// </summary>
    /// <returns>The count of active jobs.</returns>
    int GetActiveJobsCount();

     /// <summary>
    /// Get a list of all running job ids.
    /// </summary>
    /// <returns>A list of running job ids.</returns>
    IReadOnlyCollection<string> GetAllRunningJobIds();

    /// <summary>
    /// Adds or updates an active job.
    /// </summary>
    /// <param name="job">The job.</param>
    /// <param name="instance">The instancee.</param>
    void AddOrUpdateActiveJob(IJob job, IGridServerInstance instance);

    /// <summary>
    /// Is a Grid Server resource available?
    /// </summary>
    /// <param name="resourceNeeded">The resource needed.</param>
    /// <returns>A tuple of if it is available and a JobRejection reason.</returns>
    (bool isAvailable, JobRejectionReason? rejectionReason) IsResourceAvailable(GridServerResource resourceNeeded);

    /// <summary>
    /// Get the allocated resource.
    /// </summary>
    /// <returns>The allocated Grid Server resource.</returns>
    GridServerResource GetAllocatedResource();

    /// <summary>
    /// Renew a job lease.
    /// </summary>
    /// <param name="job">The job.</param>
    /// <param name="expirationInSeconds">The new expiration in seconds.</param>
    void RenewLease(IJob job, double expirationInSeconds);

    /// <summary>
    /// Create a new job on Grid Server.
    /// </summary>
    /// <param name="job">The job.</param>
    /// <param name="expirationInSeconds">The job's TTL.</param>
    /// <param name="waitForReadyInstance">Should wait for instance to become ready?</param>
    /// <param name="addToActiveJobs">Add this job to the active jobs list?</param>
    /// <returns>The SOAP interface, the instance and a job rejection reason.</returns>
    /// <exception cref="Exception">Cannot create a new job, since job already exists</exception>
    (ComputeCloudServiceSoap soapInterface, IGridServerInstance instance, JobRejectionReason? rejectionReason) NewJob(
        IJob job,
        double expirationInSeconds,
        bool waitForReadyInstance = false,
        bool addToActiveJobs = true
    );

    /// <summary>
    /// Get the SOAP interface for a job.
    /// </summary>
    /// <param name="job">The job.</param>
    /// <returns>The SOAP interface associated with the job.</returns>
    /// <exception cref="Exception">
    /// - Job not found.
    /// - Job found but the instance has already exited.
    /// </exception>
    ComputeCloudServiceSoap GetJob(IJob job);

    /// <summary>
    /// Close a job.
    /// </summary>
    /// <param name="job">The job.</param>
    /// <param name="attemptToRecycle">Attempt a GC.</param>
    void CloseJob(IJob job, bool attemptToRecycle);

    /// <summary>
    /// Get the current Grid Server version.
    /// </summary>
    /// <returns>The Grid Server version.</returns>
    string GetVersion();

    /// <summary>
    /// Gets the unexpectedly closed game jobs.
    /// </summary>
    /// <returns></returns>
    IReadOnlyCollection<GameJob> GetUnexpectedExitGameJobs();

    /// <summary>
    /// Dispatch a SOAP message to every running job.
    /// </summary>
    /// <param name="action">The SOAP message.</param>
    void DispatchRequestToAllActiveJobs(Action<ComputeCloudServiceSoap> action);

    /// <summary>
    /// Get the instance id for an Grid Server by it's job id.
    /// </summary>
    /// <param name="jobId">The job id.</param>
    /// <returns>The Grid Server instance id.</returns>
    string GetGridServerInstanceId(string jobId);

    /// <summary>
    /// Update an Grid Server instance's resources.
    /// </summary>
    /// <param name="job">The resources job.</param>
    /// <returns>If the update was successful or not.</returns>
    bool UpdateGridServerInstance(GridServerResourceJob job);
}
