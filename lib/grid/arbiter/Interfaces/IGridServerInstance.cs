/*

    File name: IGridServerInstance.cs
    Written By: @networking-owk
    Description: Represents the base interface for a grid server instance to be consumed by the arbiter.

    Copyright MFDLABS 2001-2022. All rights reserved.

*/

namespace MFDLabs.Grid;

using System;
using System.ServiceModel;
using System.Threading.Tasks;
using System.ServiceModel.Description;

using ComputeCloud;

/// <summary>
/// Base class for a grid server instance to be consumed by the arbiter.
/// </summary>
public interface IGridServerInstance : IDisposable
{
    /// <summary>
    /// Get the name of the instance.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Get the value indicating whether the instance is available for use.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Gets the open state of the instance.
    /// </summary>
    bool IsOpened { get; }

    /// <summary>
    /// Get the value that determines if this instance is persistent and should not be disposed after use.
    /// </summary>
    bool Persistent { get; }

    /// <summary>
    /// Gets the value that determines if this instance can be used in the arbiter pool.
    /// </summary>
    bool IsPoolable { get; }

    /// <summary>
    /// Gets the value that determines if this instance is disposed.
    /// </summary>
    bool IsDisposed { get; }

    /// <inheritdoc cref="ClientBase{TChannel}.Endpoint"/>
    ServiceEndpoint Endpoint { get; }

    /// <summary>
    /// Gets the <see cref="IGridServerProcess"/> deployed by a <see cref="IGridServerDeployer"/>
    /// </summary>
    IGridServerProcess Process { get; }

    #region |Lifecycle Management|

    /// <summary>
    /// Try to start the grid-server process. 
    /// 
    /// This is virtual because the remote-managed arbiter calls on InstanceManagementAPI.StartInstance().
    /// </summary>
    /// <returns>True if the process was started, false otherwise. False should be treated as a failure.</returns>
    bool TryStart();

    /// <summary>
    /// Unlock the instance for compute access.
    /// </summary>
    void Unlock();

    /// <summary>
    /// Lock the instance to prevent compute access.
    /// </summary>
    void Lock();

    /// <summary>
    /// Lock the instance and try start it.
    /// </summary>
    void LockAndTryStart();

    /// <summary>
    /// Wait for the instance to be available.
    /// </summary>
    /// <param name="timeout">The timeout to wait for the instance to be available.</param>
    /// <returns>True if the instance is available, false otherwise.</returns>
    bool WaitForAvailable(TimeSpan timeout);

    /// <summary>
    /// Gets a different process instance.
    /// </summary>
    /// <param name="force">Force a new process instance. Basically bypasses waiting for the instance to be available.</param>
    /// <returns>True if the process was started, false otherwise. False should be treated as a failure.</returns>
    bool TryStartNewProcess(bool force = false);

    #endregion |Lifecycle Management|

    #region |SOAP Methods|


    /// <summary>
    /// Invoke a HelloWorld call to the grid server.
    /// </summary>
    /// <returns>A hello world string.</returns>
    string HelloWorld();

    /// <summary>
    /// Invoke a HelloWorld call to the grid server asynchronously.
    /// </summary>
    /// <returns>A hello world string.</returns>
    Task<string> HelloWorldAsync();


    /// <summary>
    /// Get the version of the grid server.
    /// </summary>
    /// <returns>A version string.</returns>
    string GetVersion();

    /// <summary>
    /// Get the version of the grid server asynchronously.
    /// </summary>
    /// <returns>A version string.</returns>
    Task<string> GetVersionAsync();


    /// <summary>
    /// Get the status of the grid server.
    /// </summary>
    /// <returns>The status of the grid server.</returns>
    Status GetStatus();

    /// <summary>
    /// Get the status of the grid server asynchronously.
    /// </summary>
    /// <returns>The status of the grid server.</returns>
    Task<Status> GetStatusAsync();


    /// <summary>
    /// Open a new <see cref="Job"/> on the grid server.
    /// </summary>
    /// <param name="job">Information on the <see cref="Job"/>.</param>
    /// <param name="script">The initialization script.</param>
    /// <returns>The result of the initialization script.</returns>
    [Obsolete($"{nameof(OpenJob)} is deprecated, use {nameof(OpenJobEx)} instead.")]
    LuaValue[] OpenJob(Job job, ScriptExecution script);

    /// <summary>
    /// Open a new <see cref="Job"/> on the grid server asynchronously.
    /// </summary>
    /// <param name="job">Information on the <see cref="Job"/>.</param>
    /// <param name="script">The initialization script.</param>
    /// <returns>The result of the initialization script.</returns>
    [Obsolete($"{nameof(OpenJobAsync)} is deprecated, use {nameof(OpenJobExAsync)} instead.")]
    Task<OpenJobResponse> OpenJobAsync(Job job, ScriptExecution script);


    /// <summary>
    /// Open a new <see cref="Job"/> on the grid server.
    /// </summary>
    /// <param name="job">Information on the <see cref="Job"/>.</param>
    /// <param name="script">The initialization script.</param>
    /// <returns>The result of the initialization script.</returns>
    LuaValue[] OpenJobEx(Job job, ScriptExecution script);

    /// <summary>
    /// Open a new <see cref="Job"/> on the grid server asynchronously.
    /// </summary>
    /// <param name="job">Information on the <see cref="Job"/>.</param>
    /// <param name="script">The initialization script.</param>
    /// <returns>The result of the initialization script.</returns>
    Task<LuaValue[]> OpenJobExAsync(Job job, ScriptExecution script);


    /// <summary>
    /// Renew the lease of a <see cref="Job"/> within the grid server.
    /// </summary>
    /// <param name="jobId">The ID of the <see cref="Job"/>.</param>
    /// <param name="expirationInSeconds">The new expiration in seconds.</param>
    /// <returns>The new expiration in seconds.</returns>
    double RenewLease(string jobId, double expirationInSeconds);

    /// <summary>
    /// Renew the lease of a <see cref="Job"/> within the grid server asynchronously.
    /// </summary>
    /// <param name="jobId">The ID of the <see cref="Job"/>.</param>
    /// <param name="expirationInSeconds">The new expiration in seconds.</param>
    /// <returns>The new expiration in seconds.</returns>
    Task<double> RenewLeaseAsync(string jobId, double expirationInSeconds);


    /// <summary>
    /// Execute a <see cref="ScriptExecution"/> on a <see cref="Job"/> on the grid server.
    /// </summary>
    /// <param name="jobId">The ID of the <see cref="Job"/>.</param>
    /// <param name="script">The scrip to execute.</param>
    /// <returns>The result of the script.</returns>
    [Obsolete($"{nameof(Execute)} is deprecated, use {nameof(ExecuteEx)} instead.")]
    LuaValue[] Execute(string jobId, ScriptExecution script);

    /// <summary>
    /// Execute a <see cref="ScriptExecution"/> on a <see cref="Job"/> on the grid server asynchronously.
    /// </summary>
    /// <param name="jobId">The ID of the <see cref="Job"/>.</param>
    /// <param name="script">The scrip to execute.</param>
    /// <returns>The result of the script.</returns>
    [Obsolete($"{nameof(ExecuteAsync)} is deprecated, use {nameof(ExecuteExAsync)} instead.")]
    Task<ExecuteResponse> ExecuteAsync(string jobId, ScriptExecution script);


    /// <summary>
    /// Execute a <see cref="ScriptExecution"/> on a <see cref="Job"/> on the grid server.
    /// </summary>
    /// <param name="jobId">The ID of the <see cref="Job"/>.</param>
    /// <param name="script">The scrip to execute.</param>
    /// <returns>The result of the script.</returns>
    LuaValue[] ExecuteEx(string jobId, ScriptExecution script);

    /// <summary>
    /// Execute a <see cref="ScriptExecution"/> on a <see cref="Job"/> on the grid server asynchronously.
    /// </summary>
    /// <param name="jobId">The ID of the <see cref="Job"/>.</param>
    /// <param name="script">The scrip to execute.</param>
    /// <returns>The result of the script.</returns>
    Task<LuaValue[]> ExecuteExAsync(string jobId, ScriptExecution script);


    /// <summary>
    /// Close a <see cref="Job"/> on the grid server.
    /// </summary>
    /// <param name="jobId">The ID of the <see cref="Job"/>.</param>
    void CloseJob(string jobId);

    /// <summary>
    /// Close a <see cref="Job"/> on the grid server asynchronously.
    /// </summary>
    /// <param name="jobId">The ID of the <see cref="Job"/>.</param>
    /// <returns>An awaitable Task.</returns>
    Task CloseJobAsync(string jobId);


    /// <summary>
    /// Execute a <see cref="ScriptExecution"/> on a new <see cref="Job"/> and then closes the <see cref="Job"/> on the grid server.
    /// </summary>
    /// <param name="job">The information on the <see cref="Job"/>.</param>
    /// <param name="script">The scrip to execute.</param>
    /// <returns>The result of the script.</returns>
    [Obsolete($"{nameof(BatchJob)} is deprecated, use {nameof(BatchJobEx)} instead.")]
    LuaValue[] BatchJob(Job job, ScriptExecution script);

    /// <summary>
    /// Execute a <see cref="ScriptExecution"/> on a new <see cref="Job"/> and then closes the <see cref="Job"/> on the grid server asynchronously.
    /// </summary>
    /// <param name="job">The information on the <see cref="Job"/>.</param>
    /// <param name="script">The scrip to execute.</param>
    /// <returns>The result of the script.</returns>
    [Obsolete($"{nameof(BatchJobAsync)} is deprecated, use {nameof(BatchJobExAsync)} instead.")]
    Task<BatchJobResponse> BatchJobAsync(Job job, ScriptExecution script);


    /// <summary>
    /// Execute a <see cref="ScriptExecution"/> on a new <see cref="Job"/> and then closes the <see cref="Job"/> on the grid server.
    /// </summary>
    /// <param name="job">The information on the <see cref="Job"/>.</param>
    /// <param name="script">The scrip to execute.</param>
    /// <returns>The result of the script.</returns>
    LuaValue[] BatchJobEx(Job job, ScriptExecution script);

    /// <summary>
    /// Execute a <see cref="ScriptExecution"/> on a new <see cref="Job"/> and then closes the <see cref="Job"/> on the grid server asynchronously.
    /// </summary>
    /// <param name="job">The information on the <see cref="Job"/>.</param>
    /// <param name="script">The scrip to execute.</param>
    /// <returns>The result of the script.</returns>
    Task<LuaValue[]> BatchJobExAsync(Job job, ScriptExecution script);


    /// <summary>
    /// Get the expiration of a <see cref="Job"/> on the grid server.
    /// </summary>
    /// <param name="jobId">The ID of the <see cref="Job"/>.</param>
    /// <returns>The expiration of the <see cref="Job"/> in seconds.</returns>
    double GetExpiration(string jobId);

    /// <summary>
    /// Get the expiration of a <see cref="Job"/> on the grid server asynchronously.
    /// </summary>
    /// <param name="jobId">The ID of the <see cref="Job"/>.</param>
    /// <returns>The expiration of the <see cref="Job"/> in seconds.</returns>
    Task<double> GetExpirationAsync(string jobId);

    
    /// <summary>
    /// Get all the <see cref="Job"/>s on the grid server.
    /// </summary>
    /// <returns>The <see cref="Job"/>s on the grid server.</returns>
    [Obsolete($"{nameof(GetAllJobs)} is deprecated, use {nameof(GetAllJobs)} instead.")]
    Job[] GetAllJobs();

    /// <summary>
    /// Get all the <see cref="Job"/>s on the grid server asynchronously.
    /// </summary>
    /// <returns>The <see cref="Job"/>s on the grid server.</returns>
    [Obsolete($"{nameof(GetAllJobsAsync)} is deprecated, use {nameof(GetAllJobsAsync)} instead.")]
    Task<GetAllJobsResponse> GetAllJobsAsync();

    
    /// <summary>
    /// Get all the <see cref="Job"/>s on the grid server.
    /// </summary>
    /// <returns>The <see cref="Job"/>s on the grid server.</returns>
    Job[] GetAllJobsEx();

    /// <summary>
    /// Get all the <see cref="Job"/>s on the grid server asynchronously.
    /// </summary>
    /// <returns>The <see cref="Job"/>s on the grid server.</returns>
    Task<Job[]> GetAllJobsExAsync();

    
    /// <summary>
    /// Close all the expired <see cref="Job"/>s on the grid server.
    /// </summary>
    /// <returns>The number of <see cref="Job"/>s closed.</returns>
    int CloseExpiredJobs();

    /// <summary>
    /// Close all the expired <see cref="Job"/>s on the grid server asynchronously.
    /// </summary>
    /// <returns>The number of <see cref="Job"/>s closed.</returns>
    Task<int> CloseExpiredJobsAsync();

    
    /// <summary>
    /// Close all the <see cref="Job"/>s on the grid server.
    /// </summary>
    /// <returns>The number of <see cref="Job"/>s closed.</returns>
    int CloseAllJobs();

    /// <summary>
    /// Close all the <see cref="Job"/>s on the grid server asynchronously.
    /// </summary>
    /// <returns>The number of <see cref="Job"/>s closed.</returns>
    Task<int> CloseAllJobsAsync();

    
    /// <summary>
    /// Get the diagnostics of a <see cref="Job"/> on the grid server.
    /// </summary>
    /// <remarks>
    /// In post-2018 grid servers, this method will be disabled by default.
    /// 
    /// To mock this post-2018 behavior, use <see cref="Execute(string, ScriptExecution)"/> supplying the diagnostic information to fetch.
    /// </remarks>
    /// <param name="type">The type of diagnostics to get.</param>
    /// <param name="jobId">The ID of the <see cref="Job"/>.</param>
    /// <returns>The diagnostics of the <see cref="Job"/> in the form of <see cref="LuaValue"/>.</returns>
    [Obsolete($"{nameof(Diag)} is deprecated, use {nameof(DiagEx)} instead.")]
    LuaValue[] Diag(int type, string jobId);

    /// <summary>
    /// Get the diagnostics of a <see cref="Job"/> on the grid server asynchronously.
    /// </summary>
    /// <remarks>
    /// In post-2018 grid servers, this method will be disabled by default.
    /// 
    /// To mock this post-2018 behavior, use <see cref="ExecuteAsync(string, ScriptExecution)"/> supplying the diagnostic information to fetch.
    /// </remarks>
    /// <param name="type">The type of diagnostics to get.</param>
    /// <param name="jobId">The ID of the <see cref="Job"/>.</param>
    /// <returns>The diagnostics of the <see cref="Job"/> in the form of <see cref="LuaValue"/>.</returns>
    [Obsolete($"{nameof(DiagAsync)} is deprecated, use {nameof(DiagExAsync)} instead.")]
    Task<DiagResponse> DiagAsync(int type, string jobId);

    
    /// <summary>
    /// Get the diagnostics of a <see cref="Job"/> on the grid server.
    /// </summary>
    /// <remarks>
    /// In post-2018 grid servers, this method will be disabled by default.
    /// 
    /// To mock this post-2018 behavior, use <see cref="Execute(string, ScriptExecution)"/> supplying the diagnostic information to fetch.
    /// </remarks>
    /// <param name="type">The type of diagnostics to get.</param>
    /// <param name="jobId">The ID of the <see cref="Job"/>.</param>
    /// <returns>The diagnostics of the <see cref="Job"/> in the form of <see cref="LuaValue"/>.</returns>
    LuaValue[] DiagEx(int type, string jobId);

    /// <summary>
    /// Get the diagnostics of a <see cref="Job"/> on the grid server asynchronously.
    /// </summary>
    /// <remarks>
    /// In post-2018 grid servers, this method will be disabled by default.
    /// 
    /// To mock this post-2018 behavior, use <see cref="ExecuteAsync(string, ScriptExecution)"/> supplying the diagnostic information to fetch.
    /// </remarks>
    /// <param name="type">The type of diagnostics to get.</param>
    /// <param name="jobId">The ID of the <see cref="Job"/>.</param>
    /// <returns>The diagnostics of the <see cref="Job"/> in the form of <see cref="LuaValue"/>.</returns>
    Task<LuaValue[]> DiagExAsync(int type, string jobId);


    #endregion |SOAP Methods|
}
