namespace Grid;

using System;
using System.Threading;
using System.Threading.Tasks;

using Docker.DotNet;
using Docker.DotNet.Models;

using Newtonsoft.Json;

using Logging;

/// <summary>
/// Represents the default docker authority for Grid Server.
/// </summary>
public class GridServerDockerAuthority
{
    /// <summary>
    /// The default CPU scheduler period.
    /// </summary>
    public const long DefaultSchedulerCpuPeriod = 100000;

    /// <summary>
    /// Represents the double epsilon number.
    /// </summary>
    public const double _DoubleEpsilon = 1E-05;

    private const long _DefaultPhysicalCoreToLogicalCoreRatio = 2;
    private const byte _MaxAttempts = 10;

    private static readonly TimeSpan _CreateImageRetrySleepIntervalBase = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan _MaxSleepInterval = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan _TimeToSleepBetweenStopContainerAttempts = TimeSpan.FromMilliseconds(100);

    private readonly long _PhysicalCoreToLogicalCoreRatio;
    private readonly ILogger _Logger;
    private readonly IGridServerDockerSettings _GridServerSettings;
    private readonly HasExitedOperation _HasExitedOperation;
    private readonly CheckImageOperation _CheckImageOperation;
    private readonly CreateImageOperation _CreateImageOperation;
    private readonly CreateContainerOperation _CreateContainerOperation;
    private readonly StartContainerOperation _StartContainerOperation;
    private readonly RemoveContainerOperation _RemoveContainerOperation;
    private readonly KillContainerOperation _KillContainerOperation;
    private readonly UpdateContainerOperation _UpdateContainerOperation;

    /// <summary>
    /// Constructs a new insatnce of <see cref="GridServerDockerAuthority"/>
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="dockerClient">The <see cref="DockerClient"/></param>
    /// <param name="gridServerSettings">The <see cref="IGridServerDockerSettings"/></param>
    /// <param name="serverInfo">The <see cref="IServerInfo"/></param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="logger"/> cannot be null.
    /// - <paramref name="gridServerSettings"/> cannot be null.
    /// </exception>
    internal GridServerDockerAuthority(ILogger logger, DockerClient dockerClient, IGridServerDockerSettings gridServerSettings, IServerInfo serverInfo)
    {
        _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _GridServerSettings = gridServerSettings ?? throw new ArgumentNullException(nameof(gridServerSettings));

        _HasExitedOperation = new HasExitedOperation(_Logger, dockerClient);
        _CheckImageOperation = new CheckImageOperation(_Logger, dockerClient, _GridServerSettings);
        _CreateImageOperation = new CreateImageOperation(_Logger, dockerClient, _GridServerSettings, new(logger));
        _CreateContainerOperation = new CreateContainerOperation(_Logger, dockerClient);
        _StartContainerOperation = new StartContainerOperation(_Logger, dockerClient);
        _RemoveContainerOperation = new RemoveContainerOperation(_Logger, dockerClient, _GridServerSettings);
        _KillContainerOperation = new KillContainerOperation(_Logger, dockerClient);
        _UpdateContainerOperation = new UpdateContainerOperation(_Logger, dockerClient, DockerSocketHttpClient.CreateClient(dockerClient));

        if (serverInfo != null && serverInfo.PhysicalCoreCount != 0)
        {
            _PhysicalCoreToLogicalCoreRatio = serverInfo.LogicalCoreCount / serverInfo.PhysicalCoreCount;

            return;
        }

        _PhysicalCoreToLogicalCoreRatio = _DefaultPhysicalCoreToLogicalCoreRatio;
    }

    /// <summary>
    /// Has the following container exited?
    /// </summary>
    /// <param name="containerName">The name of the container.</param>
    /// <returns>True if the container has exited.</returns>
    internal async Task<bool> HasContainerExited(string containerName) 
        => await _HasExitedOperation.ExecuteAsync(containerName);

    /// <summary>
    /// Check the Grid Server image.
    /// </summary>
    /// <param name="gridServerContainerName">The name of the container</param>
    /// <param name="version">The version of the image.</param>
    /// <returns>True if the image matches.</returns>
    internal async Task<bool> CheckImageAsync(string gridServerContainerName, string version) 
        => await _CheckImageOperation.ExecuteAsync((gridServerContainerName, version));

    /// <summary>
    /// Create an image based on an Grid Server container.
    /// </summary>
    /// <param name="gridServerContainerName">The name of the container.</param>
    /// <param name="version">The version of the image.</param>
    /// <returns>True if the image succeeded.</returns>
    internal bool CreateImageWithRetries(string gridServerContainerName, string version)
    {
        byte attempt = 1;
        while (attempt <= _MaxAttempts)
        {
            if (CreateImageAsync(gridServerContainerName, version).Result)
                return true;

            var sleepInterval = ExponentialBackoff.CalculateBackoff(
                attempt++, 
                _MaxAttempts,
                _CreateImageRetrySleepIntervalBase,
                _MaxSleepInterval,
                Jitter.Equal
            );

            _Logger.Warning(
                "CreateImageWithRetries. Failed to create new image. ContainerName: {0}. Version: {1}. Sleeping for {2} seconds", 
                gridServerContainerName, 
                version, 
                sleepInterval.TotalSeconds
            );

            Thread.Sleep(sleepInterval);
        }

        return false;
    }

    /// <summary>
    /// Create an image based on an Grid Server container.
    /// </summary>
    /// <param name="gridServerContainerName">The name of the container.</param>
    /// <param name="version">The version of the image.</param>
    /// <returns>True if the image succeeded.</returns>
    internal async Task<bool> CreateImageAsync(string gridServerContainerName, string version)
        => await _CreateImageOperation.ExecuteAsync((gridServerContainerName, version));

    /// <summary>
    /// Create a new container.
    /// </summary>
    /// <param name="createContainerParameters">The create container parameters.</param>
    /// <returns>The container id.</returns>
    internal async Task<string> CreateContainerAsync(CreateContainerParameters createContainerParameters) 
        => await _CreateContainerOperation.ExecuteAsync(createContainerParameters);

    /// <summary>
    /// Start an existing container.
    /// </summary>
    /// <param name="containerName">The name of the container.</param>
    /// <returns>True if the container started.</returns>
    internal async Task<bool> StartContainerAsync(string containerName) 
        => await _StartContainerOperation.ExecuteAsync(containerName);

    /// <summary>
    /// Remove a container with retries.
    /// </summary>
    /// <param name="containerId">The ID of the container</param>
    /// <returns>An awaitable task.</returns>
    internal async Task RemoveContainerWithRetriesAsync(string containerId)
    {
        int maxAttemptsToWaitForContainerExit = _GridServerSettings.MaxAttemptsToWaitForContainerExit;
        var sleepInterval = _GridServerSettings.ContainerStopSleepIntervalMilliseconds.HasValue 
            ? TimeSpan.FromMilliseconds(_GridServerSettings.ContainerStopSleepIntervalMilliseconds.Value) 
            : _TimeToSleepBetweenStopContainerAttempts;

        for (int attempt = 0; attempt < maxAttemptsToWaitForContainerExit; ++attempt)
        {
            if (await RemoveContainerAsync(containerId).ConfigureAwait(false))
                break;

            _Logger.Warning(
                "RemoveContainerWithRetriesAsync: RemoveContainer FAILED for {0} after {1} attempts; sleeping for {2} between retries", 
                containerId, 
                attempt, 
                sleepInterval
            );

            if (attempt == maxAttemptsToWaitForContainerExit - 1)
            {
                if (!await KillContainerAsync(containerId).ConfigureAwait(false))
                {
                    _Logger.Error("RemoveContainerWithRetriesAsync. KillContainerAsync FAILED, not going to attempt to Remove Container again");
                    break;
                }

                await RemoveContainerAsync(containerId).ConfigureAwait(false);
            }
            else
                await Task.Delay(sleepInterval);
        }
    }

    /// <summary>
    /// Remove a container.
    /// </summary>
    /// <param name="containerId">The ID of the container</param>
    /// <returns>True if the container was removed.</returns>
    internal async Task<bool> RemoveContainerAsync(string containerId) => await _RemoveContainerOperation.ExecuteAsync(containerId);

    /// <summary>
    /// Kill a container.
    /// </summary>
    /// <param name="containerId">The ID of the container</param>
    /// <returns>True if the container was killed.</returns>
    internal async Task<bool> KillContainerAsync(string containerId) => await _KillContainerOperation.ExecuteAsync(containerId);

    /// <summary>
    /// Update the container's resources.
    /// </summary>
    /// <param name="job">The Grid Server resources Job.</param>
    /// <returns>The container update response.</returns>
    internal Task<ContainerUpdateResponse> UpdateContainerAsync(GridServerResourceJob job)
    {
        var gridServerUpdateParameters = new GridServerContainerUpdateParameters
        {
            ContainerId = job.ContainerId,
            CPUPeriod = job.SchedulerCpuPeriod,
            CPUQuota = job.MaximumCores < _DoubleEpsilon 
                ? -1 
                : CalculateCpuQuota(job.MaximumCores, job.SchedulerCpuPeriod),
            Memory = job.MaximumMemoryInMegabytes * 1024 * 1024
        };

        _Logger.Information("UpdateContainerAsync. gridServerUpdateParameters: {0}", JsonConvert.SerializeObject(gridServerUpdateParameters));

        return _UpdateContainerOperation.ExecuteAsync(gridServerUpdateParameters);
    }

    /// <summary>
    /// Calculate the CPU quota.
    /// </summary>
    /// <param name="physicalCores">The physical cores.</param>
    /// <param name="cpuPeriod">The checker period.</param>
    /// <returns>The CPU quota/</returns>
    internal long CalculateCpuQuota(double physicalCores, long cpuPeriod = DefaultSchedulerCpuPeriod) 
        => (long)(physicalCores * _PhysicalCoreToLogicalCoreRatio * cpuPeriod);

    /// <summary>
    /// Calculate the physical cores available.
    /// </summary>
    /// <param name="cpuPeriod">The CPU period.</param>
    /// <param name="cpuQuota">The CPU quota</param>
    /// <returns>The physical cores.</returns>
    internal double CalculatePhysicalCores(long cpuPeriod, long cpuQuota)
    {
        if (cpuPeriod != 0)
            return cpuQuota + 0 / cpuPeriod / _PhysicalCoreToLogicalCoreRatio;

        return 0;
    }
}
