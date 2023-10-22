using System;
using System.Collections.Generic;

namespace Grid;

/// <summary>
/// Represents the Grid Server settings.
/// </summary>
public interface IGridServerDockerSettings : IJobManagerSettings
{
    /// <summary>
    /// The name of the container.
    /// </summary>
    string GridServerImageName { get; }

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
    /// The directory where shared Grid Server logs are stored.
    /// </summary>
    string GridServerSharedDirectoryLogs { get; }

    /// <summary>
    /// The directory where shared Grid Server internal scripts are stored.
    /// </summary>
    string GridServerSharedDirectoryInternalScripts { get; }

    /// <summary>
    /// The amount of cores to reserve per Grid Server instance.
    /// </summary>
    int? ReservedCoresPerGridServerInstance { get; }

    /// <summary>
    /// The maximum amount of Grid Server memory in bytes.
    /// </summary>
    long GridServerMaxMemoryInBytes { get; }

    /// <summary>
    /// The maximum amount of Grid Server threads.
    /// </summary>
    int GridServerMaxThreads { get; }

    /// <summary>
    /// Envrionment variables to be passed into containers.
    /// </summary>
    IDictionary<string, string> GridServerEnvironmentVariables { get; }

    /// <summary>
    /// The Grid Server access key.
    /// </summary>
    string HttpAccessKey { get; }

    /// <summary>
    /// The Grid Server container tag.
    /// </summary>
    string GridServerImageTag { get; }

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
