namespace Grid;

using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

using Docker.DotNet;
using Docker.DotNet.Models;

using Random;
using Logging;

/// <summary>
/// Represents the Docker implementation for <see cref="JobManagerBase"/>
/// </summary>
public class DockerJobManager : JobManagerBase
{
    private readonly Uri _DockerSocketUri = new("unix:/var/run/docker.sock");
    private readonly Uri _DockerHttpUri = new("http://host.docker.internal:2375");
    private readonly ContainersListParameters _ActiveContainerFilter;
    private readonly IGridServerDockerSettings _GridServerSettings;
    private readonly DockerClient _DockerClient;
    private readonly GridServerDockerAuthority _DockerAuthority;
    private readonly IRandom _Random;

    /// <summary>
    /// Constructs a new instance of <see cref="DockerJobManager"/>
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="portAllocator">The <see cref="IPortAllocator"/></param>
    /// <param name="rccSettings">The <see cref="IGridServerDockerSettings"/></param>
    /// <param name="random">The <see cref="IRandom"/></param>
    /// <param name="serverInfo">The <see cref="IServerInfo"/></param>
    /// <param name="resourceAllocationTracker">The <see cref="ResourceAllocationTracker"/></param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="rccSettings"/> cannot be null.
    /// - <paramref name="random"/> cannot be null.
    /// </exception>
    public DockerJobManager(
        ILogger logger,
        IPortAllocator portAllocator,
        IGridServerDockerSettings rccSettings,
        IRandom random,
        IServerInfo serverInfo = null,
        ResourceAllocationTracker resourceAllocationTracker = null
    )
        : base(logger, rccSettings, portAllocator, resourceAllocationTracker)
    {
        _GridServerSettings = rccSettings ?? throw new ArgumentNullException(nameof(rccSettings));

        _ActiveContainerFilter = new ContainersListParameters
        {
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                ["name"] = new Dictionary<string, bool> { ["/grid-server-.*-gr"] = true },
                ["status"] = new Dictionary<string, bool> { ["running"] = true },
                ["label"] = new Dictionary<string, bool> { [$"{GridServerDockerContainer.ImageNameLabel}={_GridServerSettings.GridServerImageName}"] = true }
            }
        };

        _Random = random ?? throw new ArgumentNullException(nameof(random));
        _DockerClient = CreateDockerClient();
        _DockerAuthority = new GridServerDockerAuthority(Logger, _DockerClient, _GridServerSettings, serverInfo);
    }

    /// <inheritdoc cref="JobManagerBase.GetInstanceCount"/>
    public override int GetInstanceCount() => _DockerClient.Containers.ListContainersAsync(_ActiveContainerFilter, default).Result.Count;

    /// <inheritdoc cref="JobManagerBase.GetGridServerInstanceId(string)"/>
    public override string GetGridServerInstanceId(string jobId)
    {
        ActiveJobs.TryGetValue(new Job(jobId), out var container);
        if (container == null)
            return null;

        return container.Id;
    }

    /// <inheritdoc cref="JobManagerBase.UpdateGridServerInstance(GridServerResourceJob)"/>
    public override bool UpdateGridServerInstance(GridServerResourceJob job)
    {
        try
        {
            _DockerAuthority.UpdateContainerAsync(job);
            if (ActiveJobs.TryGetValue(new Job(job.GameId), out var container))
                container.UpdateResourceLimits(job.MaximumCores, job.MaximumThreads, job.MaximumMemoryInMegabytes);

            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("Error in UpdateGridServerInstance: {0}", ex);

            return false;
        }
    }

    /// <inheritdoc cref="JobManagerBase.GetRunningActiveJobNames"/>
    protected override ISet<string> GetRunningActiveJobNames()
        => new HashSet<string>(
                (from container in ListRunningContainers()
                 select container.Names[0].Trim('/')).ToList()
           );

    /// <inheritdoc cref="JobManagerBase.GetLatestGridServerVersion"/>
    protected override string GetLatestGridServerVersion() => _GridServerSettings.GridServerImageTag;

    /// <inheritdoc cref="JobManagerBase.OnGridServerVersionChange(string, bool)"/>
    protected override bool OnGridServerVersionChange(string newGridServerVersion, bool isStartup)
    {
        if (!isStartup)
        {
            var delay = _Random.Next((int)_GridServerSettings.MaxDelayBeforeFetchingNewGridServerContainer.TotalMilliseconds);
            Logger.Information("OnGridServerVersionChange. Sleeping before fetching new Grid Server container. Sleep Duration (ms): {0}.", delay);

            Thread.Sleep(delay);
        }

        Logger.Information("OnGridServerVersionChange. Fetching new Grid Server container.");

        return _DockerAuthority.CreateImageWithRetries(_GridServerSettings.GridServerImageName, newGridServerVersion);
    }

    /// <inheritdoc cref="JobManagerBase.CreateNewGridServerInstance(int)"/>
    protected override IGridServerInstance CreateNewGridServerInstance(int port)
        => new GridServerDockerContainer(Logger, port, GridServerVersion, _GridServerSettings, _DockerAuthority, _GridServerSettings.GridServerImageName);

    /// <inheritdoc cref="JobManagerBase.FindUnexpectedExitGameJobs"/>
    protected override IReadOnlyCollection<GameJob> FindUnexpectedExitGameJobs()
    {
        var containers = ListRunningContainers();
        Logger.Information("FindUnexpectedExitGameJobs. Got {0} running containers.", containers.Count);

        var activeJobs = ActiveJobs.Cast<KeyValuePair<Job, GridServerDockerContainer>>().ToArray();
        var jobs = new Dictionary<string, GameJob>();

        foreach (var activeJobKey in activeJobs)
            if (activeJobKey.Key is GameJob job)
                jobs[activeJobKey.Value.ContainerID] = job;

        foreach (var response in containers)
            jobs.Remove(response.ID);

        return jobs.Values;
    }

    /// <inheritdoc cref="JobManagerBase.GetRunningGridServerInstances"/>
    protected override IReadOnlyCollection<IUnmanagedGridServerInstance> GetRunningGridServerInstances()
        => (from container in ListRunningContainers()
            select new UnmanagedGridServerDockerContainer(Logger, _DockerClient, _GridServerSettings, container)).ToArray();

    /// <inheritdoc cref="JobManagerBase.RecoverGridServerInstance(IUnmanagedGridServerInstance)"/>
    protected override IGridServerInstance RecoverGridServerInstance(IUnmanagedGridServerInstance instance)
    {
        if (!(instance is UnmanagedGridServerDockerContainer dockerContainer))
            return null;

        if (!dockerContainer.Container.Labels.ContainsKey(GridServerDockerContainer.PortLabel) ||
            !dockerContainer.Container.Labels.ContainsKey(GridServerDockerContainer.GridServerVersionLabel) ||
            dockerContainer.Container.Names.Count < 1)
            return null;

        var containerName = dockerContainer.Container.Names[0].Trim('/');
        var version = dockerContainer.Container.Labels[GridServerDockerContainer.GridServerVersionLabel];
        var tcpPort = Convert.ToInt32(dockerContainer.Container.Labels[GridServerDockerContainer.PortLabel]);

        var config = _DockerClient.Containers.InspectContainerAsync(instance.Id, default).Result.HostConfig;
        long maximumMemoryInMegabytes = config.Memory / 1024 / 1024;
        long cpuperiod = config.CPUPeriod;
        long cpuquota = config.CPUQuota;
        double maximumCores = _DockerAuthority.CalculatePhysicalCores(cpuperiod, cpuquota);

        Logger.Information(
            "Found a running GridServer container. Container ID = {0} with name {1} on TCP port: {2} with Grid Server Version: {3}, maximumCores: {4}, maximumMemoryInMegabytes: {5}",
            dockerContainer.Container.ID,
            containerName,
            tcpPort,
            version,
            maximumCores,
            maximumMemoryInMegabytes
        );

        return new GridServerDockerContainer(
            Logger,
            tcpPort,
            version,
            _GridServerSettings,
            _DockerAuthority,
            containerName
        )
        {
            ContainerID = dockerContainer.Container.ID,
            ContainerName = containerName.Trim('/'),
            MaximumCores = maximumCores,
            MaximumMemoryInMegabytes = maximumMemoryInMegabytes
        };
    }

    /// <inheritdoc cref="JobManagerBase.OnGetJobInstanceHasExited"/>
    protected override void OnGetJobInstanceHasExited()
    {
    }

    private IReadOnlyCollection<ContainerListResponse> ListRunningContainers()
    {
        try
        {
            return _DockerClient.Containers.ListContainersAsync(_ActiveContainerFilter, default)
                .Result
                .ToList()
                .AsReadOnly();
        }
        catch (Exception ex)
        {
            Logger.Error("ListRunningContainers: {0}", ex);

            return Array.Empty<ContainerListResponse>();
        }
    }

    private DockerClient CreateDockerClient()
    {
        try
        {
            Logger.Information("Trying to connect to {0}", _DockerSocketUri);

            var client = new DockerClientConfiguration(_DockerSocketUri).CreateClient();
            var versionResponse = client.System.GetVersionAsync().GetAwaiter().GetResult();

            Logger.Information("Connected to docker version {0}", versionResponse.Version);

            return client;
        }
        catch (Exception exc)
        {
            Logger.Error("Unable to connect to Docker socket at {0}, will try to fall back to {1}: {2}", _DockerSocketUri, _DockerHttpUri, exc);

            try
            {
                Logger.Information("Trying to connect to {0}", _DockerHttpUri);

                var client = new DockerClientConfiguration(_DockerHttpUri).CreateClient();
                var versionResponse = client.System.GetVersionAsync().GetAwaiter().GetResult();

                Logger.Information("Connected to docker version {0}", versionResponse.Version);

                return client;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Unable to connect to Docker socket at {0}: {1}", _DockerHttpUri, ex));
            }
        }
    }
}
