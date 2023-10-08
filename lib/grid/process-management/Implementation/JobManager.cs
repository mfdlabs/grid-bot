namespace Grid;

using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

using Logging;

/// <summary>
/// Represents the Process implementation for <see cref="JobManagerBase{TInstance, TUnmanagedInstance}"/>
/// </summary>
public class ProcessJobManager : JobManagerBase<GridServerProcess, UnmanagedGridServerProcess>
{
    private readonly IGridServerProcessSettings _GridServerSettings;
    private readonly IGridServerFileHelper _FileHelper;

    /// <summary>
    /// Constructs a new instance of <see cref="ProcessJobManager"/>
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="portAllocator">The <see cref="IPortAllocator"/></param>
    /// <param name="gridServerSettings">The <see cref="IGridServerProcessSettings"/></param>
    /// <param name="gridServerFileHelper">The <see cref="IGridServerFileHelper"/></param>
    /// <param name="resourceAllocationTracker">The <see cref="ResourceAllocationTracker"/></param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="gridServerSettings"/> cannot be null.
    /// </exception>
    public ProcessJobManager(
        ILogger logger,
        IPortAllocator portAllocator,
        IGridServerProcessSettings gridServerSettings,
        ResourceAllocationTracker resourceAllocationTracker,
        IGridServerFileHelper gridServerFileHelper = null
    )
        : base(logger, gridServerSettings, portAllocator, resourceAllocationTracker)
    {
        _GridServerSettings = gridServerSettings ?? throw new ArgumentNullException(nameof(gridServerSettings));

        _FileHelper = gridServerFileHelper ?? new GridServerFileHelper(gridServerSettings);
    }

    /// <inheritdoc cref="JobManagerBase{TInstance, TUnmanagedInstance}.GetInstanceCount"/>
    public override int GetInstanceCount()
        => ListRunningProcesses().Count;

    /// <inheritdoc cref="JobManagerBase{TInstance, TUnmanagedInstance}.GetGridServerInstanceId(string)"/>
    public override string GetGridServerInstanceId(string jobId)
    {
        ActiveJobs.TryGetValue(new Job(jobId), out var container);
        if (container == null)
            return null;

        return container.Id;
    }

    /// <inheritdoc cref="JobManagerBase{TInstance, TUnmanagedInstance}.UpdateGridServerInstance(GridServerResourceJob)"/>
    public override bool UpdateGridServerInstance(GridServerResourceJob job)
    {
        try
        {
            if (ActiveJobs.TryGetValue(new Job(job.GameId), out var process))
                process.UpdateResourceLimits(job.MaximumCores, job.MaximumThreads, job.MaximumMemoryInMegabytes);

            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("Error in UpdateGridServerInstance: {0}", ex);

            return false;
        }
    }

    /// <inheritdoc cref="JobManagerBase{TInstance, TUnmanagedInstance}.GetRunningActiveJobNames"/>
    protected override ISet<string> GetRunningActiveJobNames()
        => new HashSet<string>(
                (from process in ListRunningProcesses()
                 select process.RawProcess.Id.ToString()).ToList()
           );

    /// <inheritdoc cref="JobManagerBase{TInstance, TUnmanagedInstance}.GetLatestGridServerVersion"/>
    protected override string GetLatestGridServerVersion() => ReadRccVersion();

    private string ReadRccVersion()
    {
        var fileVersionInfo = FileVersionInfo.GetVersionInfo(_FileHelper.GetFullyQualifiedGridServerPath());

        return fileVersionInfo.FileVersion;
    }

    /// <inheritdoc cref="JobManagerBase{TInstance, TUnmanagedInstance}.OnGridServerVersionChange(string, bool)"/>
    protected override bool OnGridServerVersionChange(string newGridServerVersion, bool isStartup)
        => true;

    /// <inheritdoc cref="JobManagerBase{TInstance, TUnmanagedInstance}.CreateNewGridServerInstance(int)"/>
    protected override GridServerProcess CreateNewGridServerInstance(int port)
        => new(Logger, port, GridServerVersion, _GridServerSettings, new RawGridServerProcess(), _FileHelper);

    /// <inheritdoc cref="JobManagerBase{TInstance, TUnmanagedInstance}.FindUnexpectedExitGameJobs"/>
    protected override IReadOnlyCollection<GameJob> FindUnexpectedExitGameJobs()
    {
        var processes = ListRunningProcesses();
        Logger.Information("FindUnexpectedExitGameJobs. Got {0} running processes.", processes.Count);

        var activeJobs = ActiveJobs.ToArray();
        var jobs = new Dictionary<string, GameJob>();

        foreach (var activeJobKey in activeJobs)
            if (activeJobKey.Key is GameJob job)
                jobs[activeJobKey.Value.Id] = job;

        foreach (var response in processes)
            jobs.Remove(response.RawProcess.Id.ToString());

        return jobs.Values;
    }

    /// <inheritdoc cref="JobManagerBase{TInstance, TUnmanagedInstance}.GetRunningGridServerInstances"/>
    protected override IReadOnlyCollection<UnmanagedGridServerProcess> GetRunningGridServerInstances()
        => (from process in ListRunningProcesses()
            select new UnmanagedGridServerProcess(Logger, process)).ToArray();

    /// <inheritdoc cref="JobManagerBase{TInstance, TUnmanagedInstance}.RecoverGridServerInstance(TUnmanagedInstance)"/>
    protected override GridServerProcess RecoverGridServerInstance(UnmanagedGridServerProcess instance)
    {
        if (instance.Process.HasExited) return null;

        var name = instance.Process.RawProcess.Id.ToString();
        var port = instance.Process.EndPoint.Port;

        Logger.Information(
            "Found a running GridServer process. Process ID = {0} on TCP port: {1}",
            instance.Process.RawProcess.Id,
            port
        );

        return new GridServerProcess(
            Logger,
            port,
            GetVersion(),
            _GridServerSettings,
            instance.Process,
            _FileHelper
        )
        {
            ProcessName = name
        };
    }

    /// <inheritdoc cref="JobManagerBase{TInstance, TUnmanagedInstance}.OnGetJobInstanceHasExited"/>
    protected override void OnGetJobInstanceHasExited()
    {
    }

    private IReadOnlyCollection<IGridServerProcess> ListRunningProcesses()
    {
        try
        {
            var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(_GridServerSettings.GridServerExecutableName));

            return processes.Where(p => p.GetProcessEndPoint(out _)).Select(p =>
            {
                p.GetProcessEndPoint(out var endpoint);

                return new RawGridServerProcess(p, endpoint);
            }).ToArray();
        }
        catch (Exception ex)
        {
            Logger.Error("ListRunningProcesses: {0}", ex);

            return Array.Empty<IGridServerProcess>();
        }
    }
}
