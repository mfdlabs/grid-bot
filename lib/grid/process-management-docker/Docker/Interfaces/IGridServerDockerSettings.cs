namespace Grid.ProcessManagement.Docker;

using System;
using System.Collections.Generic;

using Core;

/// <summary>
/// Represents the Grid Server settings.
/// </summary>
public interface IGridServerDockerSettings : IJobManagerSettings
{
    /// <summary>
    /// Gets the base url for Grid Server
    /// </summary>
    string BaseUrl { get; set; }

    /// <summary>
    /// The name of the application.
    /// </summary>
    string GridServerApplicationName { get; set; }

    /// <summary>
    /// The name of the container.
    /// </summary>
    string GridServerImageName { get; }

    /// <summary>
    /// The Grid Server container tag.
    /// </summary>
    string GridServerImageTag { get; }

    /// <summary>
    /// Docker registry username.
    /// </summary>
    string DockerRegistryUsername { get; }

    /// <summary>
    /// Docker registry password.
    /// </summary>
    string DockerRegistryPassword { get; }

    /// <summary>
    /// Docker registry identity token.
    /// </summary>
    string DockerRegistryIdentityToken { get; }

    /// <summary>
    /// Is remove volumes enabled?
    /// </summary>
    bool? IsRemoveVolumesEnabled { get; }

    /// <summary>
    /// Sleep interval for container stop operations.
    /// </summary>
    int? ContainerStopSleepIntervalMilliseconds { get; }

    /// <summary>
    /// The override for the mount path.
    /// </summary>
    string MountPathOverride { get; }

    /// <summary>
    /// Maximum delay before fetching new Grid Server containers.
    /// </summary>
    TimeSpan MaxDelayBeforeFetchingNewGridServerContainer { get; }

    /// <summary>
    /// The directory where shared Grid Server Service logs are stored.
    /// </summary>
    string GridServerSharedDirectoryLogs { get; }

    /// <summary>
    /// The name of the file to cache app settings in.
    /// </summary>
    string GridServerApplicationSettingsFileName { get; set; }

    /// <summary>
    /// The directory where shared app data is stored.
    /// </summary>
    string GridServerSharedDirectoryAppData { get; set; }

#if !GRID_SERVER_FOR_WINE
    /// <summary>
    /// The directory where shared Grid Server cache is stored.
    /// </summary>
    string GridServerSharedDirectoryCache { get; set; }

    /// <summary>
    /// The directory where shared temp files are stored.
    /// </summary>
    string GridServerSharedDirectoryTemp { get; set; }

    /// <summary>
    /// Is Grid Server's UDP port range enabled?
    /// </summary>
    bool IsGridServerUdpLimitedPortRangeEnabled { get; set; }

    /// <summary>
    /// Starting port for Grid Server containers.
    /// </summary>
    int? GridServerContainerStartingPort { get; set; }

    /// <summary>
    /// Ending port for Grid Server containers.
    /// </summary>
    int? GridServerContainerEndingPort { get; set; }

    /// <summary>
    /// Is pass UDP port range to Grid Server enabled?
    /// </summary>
    bool IsPassUdpPortRangeToGridServerEnabled { get; set; }
#endif

    /// <summary>
    /// The amount of cores to reserve per Grid Server instance.
    /// </summary>
    int? ReservedCoresPerGridServerInstance { get; }

    /// <summary>
    /// The maximum amount of Grid Server memory in bytes.
    /// </summary>
    long GridServerMaxMemoryInBytes { get; }

    /// <summary>
    /// Envrionment variables to be passed into containers.
    /// </summary>
    IDictionary<string, string> GridServerEnvironmentVariables { get; }

    /// <summary>
    /// The Grid Server access key.
    /// </summary>
    string HttpAccessKey { get; }

    /// <summary>
    /// Primary DNS server for Grid Server containers.
    /// </summary>
    string GridServerPrimaryDnsServer { get; }

    /// <summary>
    /// Secondary DNS server for Grid Server containers.
    /// </summary>
    string GridServerSecondaryDnsServer { get; }

    /// <summary>
    /// Containers wait time before killing in seconds.
    /// </summary>
    int ContainerStopWaitBeforeKillInSeconds { get; }

    /// <summary>
    /// Max attempts to wait for containers to exit.
    /// </summary>
    int MaxAttemptsToWaitForContainerExit { get; }

    /// <summary>
    /// The Grid Server settings key.
    /// </summary>
    string GridServerSettingsKey { get; }

    /// <summary>
    /// Represents the maximum time to wait for the image to be downloaded.
    /// </summary>
    TimeSpan MaxTimeToWaitForImage { get; }

    /// <summary>
    /// Represents the maximum time to wait for the image to be inspected.
    /// </summary>
    TimeSpan MaxTimeToWaitForInspectImage { get; }
}
