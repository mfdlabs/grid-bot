namespace Grid.ProcessManagement.Core;

using System;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Logging;

using Client;
using Diagnostics;
using PortManagement;
using ClientSettings.Client;

/// <summary>
/// Represents the base abstract class for all job managers.
/// </summary>
public abstract class JobManagerBase
{
    private const int _MillisecondsInSecond = 1000;
    private const int _DefaultIntervalToQueryForRunningJobs = 3000;
    private const int _DefaultGridServerJobTimeoutInMilliseconds = 300000;
    private const int _DefaultTimeToCheckForNewGridServerVersion = 10000;
    private const int _DefaultTimeToFetchGridServerApplicationSettings = 30000;
    private const int _DefaultManagePopulateGridServerIntervalMilliseconds = 10000;
    private const int _DefaultPopulateInstanceSleepInterval = 100;
    private const int _DefaultExpirationForRecoveredInstances = 300000;
    private const int _NegativeFive = -5;

    private static readonly TimeSpan _InitialGridServerApplicationSettingsFetchSleepIntervalBase = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan _InitialGridServerApplicationSettingsFetchSleepIntervalMax = TimeSpan.FromSeconds(15);

    /// <summary>
    /// The resource allocation tracker.
    /// </summary>
    protected readonly ResourceAllocationTracker ResourceAllocationTracker;

    /// <summary>
    /// The active jobs.
    /// </summary>
    protected readonly ConcurrentDictionary<IJob, IGridServerInstance> ActiveJobs = new();

    /// <summary>
    /// The Logger.
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// The port finder.
    /// </summary>
    protected readonly IPortAllocator PortAllocator;

    private readonly TimeSpan _ClearExpiredJobsInterval = TimeSpan.FromSeconds(3);
    private readonly object _ResourceLock = new();
    private readonly object _PopulateGridServerLock = new();

    private readonly IJobManagerSettings _Settings;
    private readonly IClientSettingsClient _ClientSettingsClient;
    private readonly ISettingsFileWriter _SettingsFileWriter;

    /// <summary>
    /// The Grid Server version.
    /// </summary>
    protected string GridServerVersion;

    /// <summary>
    /// The application settings name.
    /// </summary>
    protected string ApplicationName;

    /// <summary>
    /// The application bucket name.
    /// </summary>
    protected string BucketName;

    /// <summary>
    /// The ready instances.
    /// </summary>
    protected BlockingCollection<IGridServerInstance> ReadyInstances = new(new ConcurrentStack<IGridServerInstance>());

    private bool _IsRunning;
    private DateTime _LastSuccessfulGridServerApplicationSettingsFetchTime;

    private volatile int _PopulateThreadsWorking;
    private volatile bool _LastInstanceCreationFailed;

    /// <summary>
    /// Constructs a new instance of <see cref="JobManagerBase"/>
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="settings">The <see cref="IJobManagerSettings"/></param>
    /// <param name="portAllocator">The <see cref="IPortAllocator"/></param>
    /// <param name="clientSettingsClient">The <see cref="IClientSettingsClient"/></param>
    /// <param name="resourceAllocationTracker">The <see cref="ResourceAllocationTracker"/></param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="logger"/> cannot be null.
    /// - <paramref name="settings"/> cannot be null.
    /// - <paramref name="portAllocator"/> cannot be null.
    /// - <paramref name="clientSettingsClient"/> cannot be null.
    /// </exception>
    protected JobManagerBase(
        ILogger logger,
        IJobManagerSettings settings,
        IPortAllocator portAllocator,
        IClientSettingsClient clientSettingsClient,
        ResourceAllocationTracker resourceAllocationTracker
    ) : this(
            logger,
            settings,
            portAllocator,
            clientSettingsClient,
            new SettingsFileWriter(logger),
            resourceAllocationTracker
        )
    {
    }

    /// <summary>
    /// Constructs a new instance of <see cref="JobManagerBase"/>
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="settings">The <see cref="IJobManagerSettings"/></param>
    /// <param name="portAllocator">The <see cref="IPortAllocator"/></param>
    /// <param name="clientSettingsClient">The <see cref="IClientSettingsClient"/></param>
    /// <param name="settingsFileWriter">The <see cref="ISettingsFileWriter"/></param>
    /// <param name="resourceAllocationTracker">The <see cref="ResourceAllocationTracker"/></param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="logger"/> cannot be null.
    /// - <paramref name="settings"/> cannot be null.
    /// - <paramref name="portAllocator"/> cannot be null.
    /// - <paramref name="clientSettingsClient"/> cannot be null.
    /// - <paramref name="settingsFileWriter"/> cannot be null.
    /// </exception>
    protected JobManagerBase(
        ILogger logger,
        IJobManagerSettings settings,
        IPortAllocator portAllocator,
        IClientSettingsClient clientSettingsClient,
        ISettingsFileWriter settingsFileWriter,
        ResourceAllocationTracker resourceAllocationTracker
    )
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        PortAllocator = portAllocator ?? throw new ArgumentNullException(nameof(portAllocator));
        _ClientSettingsClient = clientSettingsClient ?? throw new ArgumentNullException(nameof(clientSettingsClient));
        _SettingsFileWriter = settingsFileWriter ?? throw new ArgumentNullException(nameof(settingsFileWriter));

        ResourceAllocationTracker = resourceAllocationTracker 
            ?? new ResourceAllocationTracker(
                ServerInfo.GetPhysicalCoreCount(Logger),
                _Settings.GridServerMaxThreads,
                (long)(ServerInfo.GetAvailablePhysicalMemoryInGigabytes(Logger) * 1024),
                () => _Settings.IsGridServerCpuAllocationCheckEnabled,
                () => _Settings.IsGridServerThreadsAllocationCheckEnabled,
                () => _Settings.IsGridServerMemoryAllocationCheckEnabled,
                () => _Settings.GridServerCpuOverAllocationRatio,
                () => _Settings.GridServerThreadsOverAllocationRatio,
                () => _Settings.GridServerMemoryOverAllocationRatio
            );
    }

    /// <summary>
    /// Get the current amount of instances.
    /// </summary>
    /// <returns>The count of instances.</returns>
    public abstract int GetInstanceCount();

    /// <summary>
    /// Get the current amount of ready instances.
    /// </summary>
    /// <returns>The count of ready instances.</returns>
    public int GetReadyInstanceCount() => ReadyInstances.Count;

    /// <summary>
    /// Get the current amount of active jobs.
    /// </summary>
    /// <returns>The count of active jobs.</returns>
    public int GetActiveJobsCount() => ActiveJobs.Count;

    /// <summary>
    /// Get a list of all running job ids.
    /// </summary>
    /// <returns>A list of running job ids.</returns>
    public IReadOnlyCollection<string> GetAllRunningJobIds()
    {
        var runningJobNames = GetRunningActiveJobNames();

        return (from job in ActiveJobs.Keys where runningJobNames.Contains(ActiveJobs[job].Name) select job.Id).ToList();
    }

    /// <summary>
    /// Start the job manager.
    /// </summary>
    public void Start()
    {
        Logger.Information("Starting JobManager using Grid.ProcessManagement.Core");

        _IsRunning = true;

        do
        {
            ReadGridServerLocation(true);
            ReadGridServerApplicationName();
        }
        while (string.IsNullOrWhiteSpace(GridServerVersion) || string.IsNullOrWhiteSpace(ApplicationName));

        RecoverRunningInstances();

        Task.Factory.StartNew(CheckGridServerVersion, TaskCreationOptions.LongRunning);
        Task.Factory.StartNew(CheckGridServerApplicationSettings, TaskCreationOptions.LongRunning);
        Task.Factory.StartNew(ManagePopulateReadyInstanceThreads, TaskCreationOptions.LongRunning);
        Task.Factory.StartNew(ClearExpiredJobs, TaskCreationOptions.LongRunning);
    }

    /// <summary>
    /// Stop the job manager.
    /// </summary>
    public void Stop()
    {
        Logger.Information("Stopping JobManager using Grid.ProcessManagement.Core");

        _IsRunning = false;
    }

    /// <summary>
    /// Adds or updates an active job.
    /// </summary>
    /// <param name="job">The job.</param>
    /// <param name="instance">The instancee.</param>
    public void AddOrUpdateActiveJob(IJob job, IGridServerInstance instance) => ActiveJobs[job] = instance;

    /// <summary>
    /// Is a Grid Server resource available?
    /// </summary>
    /// <param name="resourceNeeded">The resource needed.</param>
    /// <returns>A tuple of if it is available and a JobRejection reason.</returns>
    public (bool isAvailable, JobRejectionReason? rejectionReason) IsResourceAvailable(GridServerResource resourceNeeded)
    {
        lock (_ResourceLock)
        {
            if (ResourceAllocationTracker.IsResourceAllocationCheckEnabled())
            {
                ResourceAllocationTracker.UpdateResourceAllocation(ComputeAllocatedResource());

                return ResourceAllocationTracker.IsResourceAvailable(resourceNeeded);
            }

            return (true, null);
        }
    }

    /// <summary>
    /// Get the allocated resource.
    /// </summary>
    /// <returns>The allocated Grid Server resource.</returns>
    public GridServerResource GetAllocatedResource()
    {
        lock (_ResourceLock)
        {
            if (ResourceAllocationTracker.IsResourceAllocationCheckEnabled())
            {
                var resource = ComputeAllocatedResource();
                ResourceAllocationTracker.UpdateResourceAllocation(resource);

                return resource;
            }

            return null;
        }
    }

    /// <summary>
    /// Renew a job lease.
    /// </summary>
    /// <param name="job">The job.</param>
    /// <param name="expirationInSeconds">The new expiration in seconds.</param>
    public void RenewLease(IJob job, double expirationInSeconds)
    {
        ActiveJobs.TryGetValue(job, out var instance);

        if (instance == null || instance.HasExited) return;

        var time = DateTime.UtcNow.AddSeconds(expirationInSeconds);
        if (instance.ExpirationTime < time)
            instance.ExpirationTime = time;
    }

    /// <summary>
    /// Create a new job on Grid Server.
    /// </summary>
    /// <param name="job">The job.</param>
    /// <param name="expirationInSeconds">The job's TTL.</param>
    /// <param name="waitForReadyInstance">Should wait for instance to become ready?</param>
    /// <param name="addToActiveJobs">Add this job to the active jobs list?</param>
    /// <returns>The SOAP interface, the instance and a job rejection reason.</returns>
    /// <exception cref="Exception">annot create a new job, since job already exists</exception>
    public (GridServerServiceSoap soapInterface, IGridServerInstance instance, JobRejectionReason? rejectionReason) NewJob(
        IJob job,
        double expirationInSeconds,
        bool waitForReadyInstance = false,
        bool addToActiveJobs = true
    )
    {
        Logger.Information("NewJob. {0}, expirationInSeconds = {1}, total active jobs = {2}", job, expirationInSeconds, ActiveJobs.Count);

        if (ActiveJobs.ContainsKey(job)) throw new Exception(string.Format("Cannot create a new job, since {0} already exists", job));

        var (instance, rejectionReason) = GetReadyInstance(job.Id, expirationInSeconds, waitForReadyInstance);
        if (instance == null) return (null, default, rejectionReason);

        if (addToActiveJobs) ActiveJobs[job] = instance;

        return (instance.GetSoapInterface((int)(expirationInSeconds * _MillisecondsInSecond)), instance, null);
    }

    /// <summary>
    /// Get the SOAP interface for a job.
    /// </summary>
    /// <param name="job">The job.</param>
    /// <returns>The SOAP interface associated with the job.</returns>
    /// <exception cref="Exception">
    /// - Job not found.
    /// - Job found but the instance has already exited.
    /// </exception>
    public GridServerServiceSoap GetJob(IJob job)
    {
        Logger.Debug("GetJob. {0}, total active jobs = {1}", job, ActiveJobs.Count);

        if (!ActiveJobs.TryGetValue(job, out var instance))
            throw new Exception(string.Format("Job not found. {0}", job));

        if (instance.HasExited)
        {
            Logger.Warning("GetJob. The instance for {0} has exited.", job);

            OnGetJobInstanceHasExited();
            ActiveJobs.TryRemove(job, out _);

            throw new Exception(string.Format("Job found but the instance has already exited. {0}", job));
        }

        Logger.Debug(
            "GetJob. Found active job with {0}, GridServerVersion = {1}",
            job,
            instance.Version
        );

        return instance.GetSoapInterface(_DefaultGridServerJobTimeoutInMilliseconds);
    }

    /// <summary>
    /// Close a job.
    /// </summary>
    /// <param name="job">The job.</param>
    /// <param name="attemptToRecycle">Attempt a GC.</param>
    public void CloseJob(IJob job, bool attemptToRecycle)
    {
        Logger.Information("CloseJob. {0}", job);

        DoCloseJob(job, attemptToRecycle);
    }

    /// <summary>
    /// Get the current Grid Server version.
    /// </summary>
    /// <returns>The Grid Server version.</returns>
    public string GetVersion() => GridServerVersion;

    /// <summary>
    /// Get the current client settings application name.
    /// </summary>
    /// <returns>The client settings application name.</returns>
    public string GetApplicationName() => ApplicationName;

    /// <summary>
    /// Get the current client settings bucket name.
    /// </summary>
    /// <returns>The client settings bucket name.</returns>
    public string GetBucketName() => BucketName;

    /// <summary>
    /// Gets the unexpectedly closed game jobs.
    /// </summary>
    /// <returns></returns>
    public IReadOnlyCollection<GameJob> GetUnexpectedExitGameJobs() => FindAndCloseExitedInstances();

    /// <summary>
    /// Dispatch a SOAP message to every running job.
    /// </summary>
    /// <param name="action">The SOAP message.</param>
    public void DispatchRequestToAllActiveJobs(Action<GridServerServiceSoap> action)
    {
        foreach (var soapinstance in from instance in ActiveJobs.Values
                                     where !instance.HasExited
                                     select instance)
        {
            using var soapInterface = soapinstance.GetSoapInterface(_DefaultGridServerJobTimeoutInMilliseconds);

            action(soapInterface);
        }
    }

    /// <summary>
    /// Gets the last successful client settings fetch time.
    /// </summary>
    /// <returns>The last successful client settings fetch time.</returns>
    public DateTime GetLastSuccessfulGridServerApplicationSettingsFetchTime() => _LastSuccessfulGridServerApplicationSettingsFetchTime;

    /// <summary>
    /// Get the instance id for an Grid Server by it's job id.
    /// </summary>
    /// <param name="jobId">The job id.</param>
    /// <returns>The Grid Server instance id.</returns>
    public abstract string GetGridServerInstanceId(string jobId);

    /// <summary>
    /// Update an Grid Server instance's resources.
    /// </summary>
    /// <param name="job">The resources job.</param>
    /// <returns>If the update was successful or not.</returns>
    public abstract bool UpdateGridServerInstance(GridServerResourceJob job);

    /// <summary>
    /// Gets the latest Grid Server version.
    /// </summary>
    /// <returns>The latest Grid Server version.</returns>
    protected abstract string GetLatestGridServerVersion();

    /// <summary>
    /// Invoked when the Grid Server version is changed.
    /// </summary>
    /// <param name="newGridServerVersion">The new version.</param>
    /// <param name="isStartup">If the change was at startup.</param>
    /// <returns></returns>
    protected abstract bool OnGridServerVersionChange(string newGridServerVersion, bool isStartup);

    /// <summary>
    /// Create a new Grid Server instance with the specified port.
    /// </summary>
    /// <param name="port">The port.</param>
    /// <returns>The new Grid Server instance.</returns>
    protected abstract IGridServerInstance CreateNewGridServerInstance(int port);

    /// <summary>
    /// Find all unexpectedly exited game jobs.
    /// </summary>
    /// <returns>A list of exited game jobs.</returns>
    protected abstract IReadOnlyCollection<GameJob> FindUnexpectedExitGameJobs();

    /// <summary>
    /// Get the names of running active jobs.
    /// </summary>
    /// <returns>The names of running active jobs.</returns>
    protected abstract ISet<string> GetRunningActiveJobNames();

    /// <summary>
    /// Get all running Grid Server instances.
    /// </summary>
    /// <returns>The running Grid Server instances.</returns>
    protected abstract IReadOnlyCollection<IUnmanagedGridServerInstance> GetRunningGridServerInstances();

    /// <summary>
    /// Recover a managed Grid Server instance by an unmanaged Grid Server instance.
    /// </summary>
    /// <param name="unmanagedInstance">The unmanaged Grid Server instance.</param>
    /// <returns>The managed Grid Server instance.</returns>
    protected abstract IGridServerInstance RecoverGridServerInstance(IUnmanagedGridServerInstance unmanagedInstance);

    /// <summary>
    /// Invoked when a job exits.
    /// </summary>
    protected abstract void OnGetJobInstanceHasExited();

    /// <summary>
    /// Checks if the job manager is ready or not.
    /// </summary>
    /// <exception cref="Exception">
    /// - Grid Server Version not set
    /// </exception>
    protected void CheckForGridServerReady()
    {
        if (string.IsNullOrWhiteSpace(GridServerVersion)) throw new Exception("Grid Server Version not set");
        if (string.IsNullOrWhiteSpace(ApplicationName)) throw new Exception("Grid Server ApplicationName not set");
    }

    private void DoCloseJob(IJob job, bool attemptToRecycle)
    {
        if (!ActiveJobs.TryRemove(job, out var instance))
            return;

        Logger.Information("DoCloseJob. Removed {0} from active jobs.", job);

        if (attemptToRecycle && !instance.HasExited)
        {
            ++instance.UseCount;
            Logger.Information("DoCloseJob. Attempting to recycle instance. UseCount: {0}, Version: {1}, Instance ID: {2}", instance.UseCount, instance.Version, instance.Id);

            if (instance.UseCount < _Settings.MaxInstanceReuses)
            {
                if (instance.Version == GridServerVersion)
                {
                    Logger.Information("DoCloseJob. Recycling instance. UseCount: {0}, Version: {1}, Instance ID: {2}", instance.UseCount, instance.Version, instance.Id);

                    ReadyInstances.Add(instance);

                    return;
                }

                Logger.Information(
                    "DoCloseJob. Did not recycle instance - version out of date. CurrentVersion: {0}. Current ApplicationName: {1}. UseCount: {2}, Instance Version: {3}, Instance ID: {4}",
                    GridServerVersion,
                    ApplicationName,
                    instance.UseCount,
                    instance.Version,
                    instance.Id
                );
            }
        }

        instance.Dispose();

        Logger.Information("DoCloseJob. Killed instance. Ready instance count = {0}, total active jobs = {1}", ReadyInstances.Count, ActiveJobs.Count);
    }

    private void CheckGridServerVersion()
    {
        while (_IsRunning)
        {
            Thread.Sleep(_DefaultTimeToCheckForNewGridServerVersion);

            try
            {
                ReadGridServerLocation(false);
            }
            catch (Exception ex)
            {
                Logger.Error("CheckGridServerVersion. Error in ReadGridServerLocation. Exception: {0}", ex);
            }
        }
    }

    private void ReadGridServerLocation(bool isStartup)
    {
        var newValue = GetLatestGridServerVersion();
        if (GridServerVersion == newValue) return;

        Logger.Information("ReadGridServerLocation. GridServer version changed or loaded for the first time. GridServer version = {0}", newValue);

        if (OnGridServerVersionChange(newValue, isStartup))
        {
            Logger.Information("ReadGridServerLocation. Successfully changed GridServer version. GridServer version = {0}", newValue);

            GridServerVersion = newValue;
            KillOutOfDateReadyInstances();

            return;
        }

        Logger.Warning(
            "ReadGridServerLocation. Failed to change GridServer version. Failed GridServer Version = {0}. Current GridServer Version = {1}",
            newValue,
            GridServerVersion
        );
    }

    private void KillOutOfDateReadyInstances()
    {
        var readyInstances = new BlockingCollection<IGridServerInstance>(new ConcurrentStack<IGridServerInstance>());

        var currentReadyInstances = (IEnumerable<IGridServerInstance>)Interlocked.Exchange(ref ReadyInstances, readyInstances);

        foreach (var readyInstance in currentReadyInstances)
        {
            if (readyInstance.Version == GridServerVersion)
            {
                Logger.Information("KillOldVersionsOfReadyInstances. Found a ready instance with the current version. Exiting loop.");

                ReadyInstances.Add(readyInstance);

                continue;
            }

            try
            {
                Logger.Information(
                    "KillOldVersionsOfReadyInstances. Killed a ready instance with version: {0}",
                    readyInstance.Version
                );

                readyInstance.Dispose();

                continue;
            }
            catch (Exception ex)
            {
                Logger.Error("KillOldVersionsOfReadyInstances. Failed to kill a ready instance. Exception: {0}", ex);

                continue;
            }
        }
    }

    private bool UpdateGridServerApplicationSettingsWithRetries(string applicationName, string bucketName, byte maxAttempts)
    {
        Logger.Information("UpdateGridServerApplicationSettingsWithRetries. Fetching Grid Server application settings with {0} retries.", maxAttempts);

        var didUpdate = UpdateGridServerApplicationSettings(applicationName, bucketName);
        for (byte index = 1; !didUpdate && index <= maxAttempts; didUpdate = UpdateGridServerApplicationSettings(applicationName, bucketName))
        {
            var sleepInterval = ExponentialBackoff.CalculateBackoff(
                index++,
                10,
                _InitialGridServerApplicationSettingsFetchSleepIntervalBase,
                _InitialGridServerApplicationSettingsFetchSleepIntervalMax,
                Jitter.Equal
            );

            Logger.Warning(
                "UpdateGridServerApplicationSettingsWithRetries. Failed to update Grid Server Application Settings. ApplicationName: {0}. BucketName: {1}. Sleeping for {2} seconds",
                applicationName,
                bucketName,
                sleepInterval.TotalSeconds
            );

            Thread.Sleep(sleepInterval);
        }

        return didUpdate;
    }

    private void CheckGridServerApplicationSettings()
    {
        while (_IsRunning)
        {
            Thread.Sleep(_DefaultTimeToFetchGridServerApplicationSettings);

            try
            {
                ReadGridServerApplicationName();
                UpdateGridServerApplicationSettings(ApplicationName, BucketName);
            }
            catch (Exception ex)
            {
                Logger.Error("Error in CheckRccApplicationSettings. Exception: {0}", ex);
            }
        }
    }

    private void ReadGridServerApplicationName()
    {
        var newApplicationName = _Settings.GridServerSettingsApplicationName;
        var newBucketName = _Settings.GridServerSettingsBucketName;

        if (ApplicationName != newApplicationName || BucketName != newBucketName)
        {
            if (ApplicationName != newApplicationName)
                Logger.Information(
                    "ReadGridServerApplicationName. Grid Server ApplicationName changed or loaded for the first time. ApplicationName = {0}. Old Value = {1}.",
                    newApplicationName,
                    ApplicationName
                );

            if (BucketName != newBucketName)
                Logger.Information(
                    "ReadGridServerApplicationName. Grid Server BucketName changed or loaded for the first time. BucketName = {0}. Old Value = {1}.",
                    newBucketName,
                    BucketName
                );

            if (UpdateGridServerApplicationSettingsWithRetries(newApplicationName, newBucketName, 10))
            {
                Logger.Information(
                    "ReadGridServerApplicationName. Successfully changed Grid Server ApplicationName. ApplicationName = {0}, Old ApplicationName Value = {1}, BucketName = {2}, Old BucketName Value = {3}",
                    newApplicationName,
                    ApplicationName,
                    newBucketName,
                    BucketName
                );

                ApplicationName = newApplicationName;
                BucketName = newBucketName;

                KillOutOfDateReadyInstances();

                return;
            }

            Logger.Warning(
                "ReadGridServerApplicationName. Failed to change Grid Server ApplicationName. Failed ApplicationName = {0}, Current ApplicationName = {1}, Failed BucketName = {2}, Current BucketName = {3}",
                newApplicationName,
                ApplicationName,
                newBucketName,
                BucketName
            );
        }
    }

    /// <summary>
    /// Update the Grid Server app settings.
    /// </summary>
    /// <param name="applicationName">The application name.</param>
    /// <param name="bucketName">The bucket name.</param>
    /// <returns>If the update was successful or not.</returns>
    protected bool UpdateGridServerApplicationSettings(string applicationName, string bucketName)
    {
        try
        {
            return TryUpdateGridServerApplicationSettings(applicationName, bucketName);
        }
        catch (Exception ex)
        {
            Logger.Error("CheckGridServerApplicationSettings. Error in TryUpdateGridServerApplicationSettings. Exception: {0}", ex);
        }

        return false;
    }

    private bool TryUpdateGridServerApplicationSettings(string applicationName, string bucketName)
    {
        if (string.IsNullOrWhiteSpace(_Settings.GridServerApplicationSettingsFilePath))
        {
            Logger.Error("TryUpdateGridServerApplicationSettings. GridServerApplicationSettingsFilePath cannot be null or blank.");

            return false;
        }

        Logger.Information("TryUpdateGridServerApplicationSettings. Fetching Grid Server application settings from secured endpoint. ApplicationName: {0}. BucketName: {1}", applicationName, bucketName);

        var newGridServerApplicationSettings = _ClientSettingsClient.GetRccOnlyClientApplicationSettings(applicationName, bucketName);
        if (newGridServerApplicationSettings?.ApplicationSettings == null || newGridServerApplicationSettings.ApplicationSettings.Count == 0)
        {
            Logger.Error("TryUpdateGridServerApplicationSettings. ClientApplicationSettingsResponse was null or empty.");

            return false;
        }

        Logger.Information("TryUpdateGridServerApplicationSettings. Successfully fetched latest Grid Server application settings. Count: {0}", newGridServerApplicationSettings.ApplicationSettings.Count);
        if (!_SettingsFileWriter.WriteSettingsFile(_Settings.GridServerApplicationSettingsFilePath, newGridServerApplicationSettings))
        {
            Logger.Error("TryUpdateGridServerApplicationSettings. Error writing settings file.");

            return false;
        }

        Logger.Information("TryUpdateGridServerApplicationSettings. Successfully wrote latest GridServer application settings to file: {0}", _Settings.GridServerApplicationSettingsFilePath);

        _LastSuccessfulGridServerApplicationSettingsFetchTime = DateTime.UtcNow;

        return true;
    }

    private void ManagePopulateReadyInstanceThreads()
    {
        var ctsList = new Stack<CancellationTokenSource>(_Settings.PopulateReadyGridServerInstanceThreads);

        while (_IsRunning)
        {
            try
            {
                var desiredNumberOfThreads = _Settings.PopulateReadyGridServerInstanceThreads;

                if (ctsList.Count < desiredNumberOfThreads)
                {
                    var threadsToCreate = desiredNumberOfThreads - ctsList.Count;

                    Logger.Verbose(
                        "ManagePopulateReadyInstanceThreads. Need to create {0} threads. SettingValue = {1}, threadCount = {2}",
                        threadsToCreate,
                        desiredNumberOfThreads,
                        ctsList.Count
                    );

                    for (int i = 0; i < threadsToCreate; i++)
                    {
                        var cts = new CancellationTokenSource();
                        Task.Factory.StartNew(PopulateReadyInstances, cts.Token, TaskCreationOptions.LongRunning);

                        ctsList.Push(cts);
                    }
                }
                else if (ctsList.Count > desiredNumberOfThreads)
                {
                    var threadsToCancel = ctsList.Count - desiredNumberOfThreads;
                    Logger.Verbose(
                        "ManagePopulateReadyInstanceThreads. Need to cancel {0} threads. SettingValue = {1}, threadCount = {2}",
                        threadsToCancel,
                        desiredNumberOfThreads,
                        ctsList.Count
                    );

                    for (int j = 0; j < threadsToCancel; j++)
                        ctsList.Pop().Cancel();
                }
                else
                    Logger.Verbose("ManagePopulateReadyInstanceThreads. SettingValue = {0}, threadCount = {1}", desiredNumberOfThreads, ctsList.Count);
            }
            catch (Exception ex)
            {
                Logger.Error("Error in ManagePopulateReadyInstanceThreads. Exception: {0}", ex);
            }

            Thread.Sleep(_DefaultManagePopulateGridServerIntervalMilliseconds);
        }
    }

    private bool ShouldPopulateReadyInstance()
    {
        if (ReadyInstances.Count + _PopulateThreadsWorking >= _Settings.ReadyInstancesToKeepInReserve)
            return false;

        if (_Settings.MaxGridServerInstances != null && ReadyInstances.Count + ActiveJobs.Count + _PopulateThreadsWorking >= _Settings.MaxGridServerInstances)
            return false;

        return true;
    }

    private void PopulateReadyInstances(object state)
    {
        var threadId = Guid.NewGuid();
        var cancellationToken = (CancellationToken)state;

        while (_IsRunning && !cancellationToken.IsCancellationRequested)
        {
            if (_LastInstanceCreationFailed || !ShouldPopulateReadyInstance())
                Thread.Sleep(_DefaultPopulateInstanceSleepInterval);

            if (!ShouldPopulateReadyInstance())
                Logger.Verbose(
                    "PopulateReadyInstances. No new instance created because there are enough instances. Ready instances: {0}, PopulateThreadsWorking: {1}, Active Jobs: {2}, MaxInstances: {3}, ReadyInstancesToKeepInReserve: {4}.  ThreadId: {5}.",
                    ReadyInstances.Count,
                    _PopulateThreadsWorking,
                    ActiveJobs.Count,
                    _Settings.MaxGridServerInstances,
                    _Settings.ReadyInstancesToKeepInReserve,
                    threadId
                );
            else
            {
                lock (_PopulateGridServerLock)
                {
                    if (ShouldPopulateReadyInstance())
                        ++_PopulateThreadsWorking;
                    else
                        continue;
                }

                var instance = default(IGridServerInstance);
                var sw = Stopwatch.StartNew();

                try
                {
                    Logger.Information("PopulateReadyInstances. Creating new GridServer instance. ThreadId: {0}.", threadId);

                    CheckForGridServerReady();

                    instance = CreateNewGridServerInstance(PortAllocator.FindNextAvailablePort());

                    if (!instance.Start())
                    {
                        sw.Stop();
                        Logger.Error("PopulateReadyInstances. Failed to start GridServer instance on port {0}. ThreadId: {1}.", instance.Port, threadId);

                        _LastInstanceCreationFailed = true;

                        continue;
                    }

                    sw.Stop();
                    Logger.Information(
                        "PopulateReadyInstances. GridServer instance started and validated connectivity. Port: {0}. ThreadId: {1}. Instance ID: {2}. Version: {3}",
                        instance.Port,
                        threadId,
                        instance.Id,
                        instance.Version
                    );

                    _LastInstanceCreationFailed = false;
                    ReadyInstances.Add(instance);
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    Logger.Error("PopulateReadyInstances. Error when trying to launch a instance. ThreadId: {0}. Exception = {1}", threadId, ex);

                    instance.Dispose();
                    _LastInstanceCreationFailed = true;
                }
                finally
                {
                    lock (_PopulateGridServerLock)
                        --_PopulateThreadsWorking;
                }
            }
        }
    }

    private bool IsGridServerApplicationSettingsFileValid()
    {
        var window = DateTime.UtcNow - _Settings.GridServerApplicationSettingsValidWindow;

        return _LastSuccessfulGridServerApplicationSettingsFetchTime >= window;
    }

    private (IGridServerInstance, JobRejectionReason?) GetReadyInstance(string jobId, double expirationInSeconds, bool waitForReadyInstance)
    {
        var sw = Stopwatch.StartNew();
         if (!IsGridServerApplicationSettingsFileValid())
            return (default, JobRejectionReason.GridServerSettingsFileInvalid);

        IGridServerInstance instance;
        while (true)
        {
            if (waitForReadyInstance)
                instance = ReadyInstances.Take();
            else if (!ReadyInstances.TryTake(out instance))
                break;

            if (instance != null && instance.Version != GridServerVersion)
            {
                Logger.Information(
                    "GetReadyInstance. Killing out of date ready instance. CurrentGridServerVersion: {0}. InstanceGridServerVersion: {1}.",
                    GridServerVersion,
                    instance.Version
                );

                Task.Run(() =>
                {
                    try
                    {
                        instance.Dispose();
                    }
                    catch (Exception arg)
                    {
                        Logger.Error("GetReadyInstance. Error Disposing of instance: {0}", arg);
                    }
                });

                instance = default;
            }

            if (instance != null && !instance.HasExited)
            {
                sw.Stop();

                Logger.Information(
                    "Ready instance found, name = {0}, version = {1}, jobId = {2}, time taken = {3} ms",
                    instance.Id,
                    instance.Version,
                    jobId,
                    sw.ElapsedMilliseconds
                );

                instance.ExpirationTime = DateTime.UtcNow.AddSeconds(expirationInSeconds);

                return (instance, null);
            }
        }

        Logger.Warning("GetReadyInstance. Unable to get a ready instance because none were available");

        return (default, JobRejectionReason.NoReadyInstance);
    }

    private void ClearExpiredJobs()
    {
        while (_IsRunning)
        {
            Thread.Sleep(_ClearExpiredJobsInterval);

            var cutoffTime = DateTime.UtcNow.AddSeconds(_NegativeFive);
            var activeJobs = ActiveJobs.ToArray();

            try
            {
                for (int i = 0; i < activeJobs.Length; i++)
                {
                    var activeJob = activeJobs[i];
                    if (activeJob.Value.HasExited)
                    {
                        Logger.Warning("ClearExpiredJobs. Closing expired job {0} because GridServer instance has already exited.", activeJob.Key);

                        DoCloseJob(activeJob.Key, false);

                        continue;
                    }

                    if (activeJob.Value.ExpirationTime >= cutoffTime) continue;
                    
                    Logger.Warning(
                        "ClearExpiredJobs. Closing expired job {0} because it has expired on {1} and cutoff time is {2}",
                        activeJob.Key,
                        activeJob.Value.ExpirationTime,
                        cutoffTime
                    );

                    DoCloseJob(activeJob.Key, false);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error in ClearExpiredJobs. Exception: {0}", ex);
            }
        }
    }

    private IReadOnlyCollection<GameJob> FindAndCloseExitedInstances()
    {
        var unexpectedExitGameJobs = FindUnexpectedExitGameJobs();
        Logger.Information("FindAndCloseExitedInstances. Found {0} active game jobs whose instances have exited.", unexpectedExitGameJobs.Count);

        foreach (var job in unexpectedExitGameJobs)
        {
            try
            {
                Logger.Information("FindAndCloseExitedInstances. Closing {0}", job);

                DoCloseJob(job, false);
            }
            catch (Exception ex)
            {
                Logger.Error("FindAndCloseExitedInstances. Error Closing {0}. Exception = {1}", job, ex);
            }
        }

        return unexpectedExitGameJobs;
    }

    private void RecoverRunningInstances()
    {
        var unmanangedInstances = GetRunningGridServerInstances();
        if (unmanangedInstances == null)
        {
            Logger.Information("RecoverRunningInstances. No running GridServer instances found");
            return;
        }

        Logger.Information("RecoverRunningInstances. {0} running GridServer instances found", unmanangedInstances.Count);

        Parallel.ForEach(
            unmanangedInstances,
            unmanagedInstance =>
            {
                try
                {
                    var instance = RecoverGridServerInstance(unmanagedInstance);

                    if (instance != null)
                    {
                        instance.WaitForServiceToBecomeAvailable(true, Stopwatch.StartNew());

                        using var soapInterface = instance.GetSoapInterface(_DefaultIntervalToQueryForRunningJobs);
                        var allJobs = soapInterface.GetAllJobsEx();

                        if (allJobs.Length == 0)
                        {
                            Logger.Information("RecoverRunningInstances. No jobs found running in GridServer instance. Instance ID = {0}", instance.Id);

                            if (instance.Version != GridServerVersion)
                            {
                                Logger.Information(
                                    "RecoverRunningInstances. Recovered a ready instance with no jobs and a version mismatch. Instance ID = {0}, Port = {1}, Version = {2}, ApplicationName = {3}, BucketName = {4}, Expected Version = {5}, Expected ApplicationName = {6}, Expected BucketName = {7}",
                                    instance.Id,
                                    instance.Port,
                                    instance.Version,
                                    instance.ApplicationName,
                                    instance.BucketName,
                                    GridServerVersion,
                                    ApplicationName,
                                    BucketName
                                );

                                throw new Exception(
                                    string.Format(
                                        "Version mismatch for jobless instance. Instance ID = {0}, Port = {1}, Version = {2}, ApplicationName = {3}, BucketName = {4}, Expected Version = {5}, Expected ApplicationName = {6}, Expected BucketName = {7}",
                                        instance.Id,
                                        instance.Port,
                                        instance.Version,
                                        instance.ApplicationName,
                                        instance.BucketName,
                                        GridServerVersion,
                                        ApplicationName,
                                        BucketName
                                    )
                                );
                            }

                            ReadyInstances.Add(instance);

                            Logger.Information(
                                "RecoverRunningInstances. Recovered a ready instance. Instance ID = {0}, Port = {1}, Version = {2}, ApplicationName = {3}, BucketName = {4}",
                                instance.Id,
                                instance.Port,
                                instance.Version,
                                instance.ApplicationName,
                                instance.BucketName
                            );

                            return;
                        }

                        var job = allJobs.FirstOrDefault();
                        if (job != null)
                        {
                            Logger.Information(
                                "RecoverRunningInstances. Job found for GridServer instance. Instance ID = {0}, Job Id = {1}, Job expiration = {2}",
                                instance.Id,
                                job.id,
                                job.expirationInSeconds
                            );

                            if (Guid.TryParse(job.id, out var _))
                            {
                                var newJob = new Job(job.id);
                                ActiveJobs[newJob] = instance;
                                RenewLease(newJob, _DefaultExpirationForRecoveredInstances);

                                Logger.Information(
                                    "RecoverRunningInstances. Recovered a running job. Instance ID = {0}, Port = {1}, Job ID = {2}",
                                    instance.Id,
                                    instance.Port,
                                    job.id
                                );
                                return;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("RecoverRunningInstances. Error while attempting to recover a running GridServer instance, Exception = {0}", ex);
                }

                try
                {
                    Logger.Information("RecoverRunningInstances. Unable to recover a running GridServer instance, so it will be killed.");

                    unmanagedInstance.Kill();
                }
                catch (Exception ex)
                {
                    Logger.Error("Unable to stop and remove GridServer instance. Instance ID = {0}, Exception = {1}", unmanagedInstance.Id, ex);
                }
            }
        );
    }

    private GridServerResource ComputeAllocatedResource()
    {
        var sw = Stopwatch.StartNew();
        var runningJobNames = GetRunningActiveJobNames();
        var runningInstances = (from job in ActiveJobs.Values
                                where runningJobNames.Contains(job.Name)
                                select job).ToList();

        double cores = 0;
        long threads = 0;
        long memory = 0;
        foreach (var instance in runningInstances)
        {
            cores += instance.MaximumCores;
            threads += instance.MaximumThreads;
            memory += instance.MaximumMemoryInMegabytes;
        }

        var resourceAllocated = new GridServerResource(cores, threads, memory);

        sw.Stop();
        Logger.Warning(
            "ComputeAllocatedResource. Resource allocated: {0}. Run time in ms: {1} for {2} active rcc jobs",
            resourceAllocated,
            sw.Elapsed.TotalMilliseconds,
            runningInstances.Count
        );

        return resourceAllocated;
    }
}
