namespace Grid.Bot;

using System;
using System.IO;
using System.Collections.Generic;

using Microsoft.Extensions.Options;

using ProcessManagement;
using ProcessManagement.Core;
using ProcessManagement.Docker;

/// <summary>
/// Exposes <see cref="GridOptions"/> to the process management libraries, always reading the current value.
/// </summary>
/// <param name="options">The <see cref="GridOptions"/> monitor.</param>
/// <exception cref="ArgumentNullException"><paramref name="options"/> cannot be null.</exception>
public sealed class GridServerSettings(IOptionsMonitor<GridOptions> options) : IGridServerDockerSettings, IGridServerProcessSettings
{
    private readonly IOptionsMonitor<GridOptions> _options = options ?? throw new ArgumentNullException(nameof(options));

    private GridOptions O => _options.CurrentValue;

    /// <inheritdoc cref="IGridServerDockerSettings.GridServerImageName"/>
    public string GridServerImageName => O.GridServerImageName;

    /// <inheritdoc cref="IGridServerDockerSettings.GridServerImageTag"/>
    public string GridServerImageTag => O.GridServerImageTag;

    /// <inheritdoc cref="IGridServerDockerSettings.GridServerSettingsKey"/>
    public string GridServerSettingsKey => O.GridServerSettingsKey;

    /// <inheritdoc cref="IGridServerDockerSettings.DockerRegistryUsername"/>
    public string DockerRegistryUsername => O.DockerRegistryUsername;

    /// <inheritdoc cref="IGridServerDockerSettings.DockerRegistryPassword"/>
    public string DockerRegistryPassword => O.DockerRegistryPassword;

    /// <inheritdoc cref="IGridServerDockerSettings.DockerRegistryIdentityToken"/>
    public string DockerRegistryIdentityToken => O.DockerRegistryIdentityToken;

    /// <inheritdoc cref="IGridServerDockerSettings.IsRemoveVolumesEnabled"/>
    public bool? IsRemoveVolumesEnabled => O.IsRemoveVolumesEnabled;

    /// <inheritdoc cref="IGridServerDockerSettings.ContainerStopSleepIntervalMilliseconds"/>
    public int? ContainerStopSleepIntervalMilliseconds => O.ContainerStopSleepIntervalMilliseconds;

    /// <inheritdoc cref="IGridServerDockerSettings.MountPathOverride"/>
    public string MountPathOverride => O.MountPathOverride;

    /// <inheritdoc cref="IGridServerDockerSettings.MaxDelayBeforeFetchingNewGridServerContainer"/>
    public TimeSpan MaxDelayBeforeFetchingNewGridServerContainer => O.MaxDelayBeforeFetchingNewGridServerContainer;

    /// <inheritdoc cref="IGridServerDockerSettings.GridServerSharedDirectoryLogs"/>
    public string GridServerSharedDirectoryLogs => O.GridServerSharedDirectoryLogs;

    /// <inheritdoc cref="IGridServerDockerSettings.BaseUrl"/>
    public string BaseUrl => O.BaseUrl;

    /// <inheritdoc cref="IGridServerDockerSettings.GridServerSharedDirectoryAppData"/>
    public string GridServerSharedDirectoryAppData => O.GridServerSharedDirectoryAppData;

    /// <inheritdoc cref="IGridServerDockerSettings.GridServerAdditionalVolumeMappings"/>
    public string[] GridServerAdditionalVolumeMappings =>
    [
        .. O.GridServerAdditionalVolumeMappings,
        $"{O.GridServerSharedDirectoryInternalScripts}:{O.GridServerInsideDirectoryInternalScripts}"
    ];

    /// <inheritdoc cref="IGridServerDockerSettings.ReservedCoresPerGridServerInstance"/>
    public int? ReservedCoresPerGridServerInstance => O.ReservedCoresPerGridServerInstance;

    /// <inheritdoc cref="IGridServerDockerSettings.GridServerMaxMemoryInBytes"/>
    public long GridServerMaxMemoryInBytes => O.GridServerMaxMemoryInBytes;

    /// <inheritdoc cref="IJobManagerSettings.GridServerMaxThreads"/>
    public int GridServerMaxThreads => O.GridServerMaxThreads;

    /// <inheritdoc cref="IGridServerDockerSettings.GridServerEnvironmentVariables"/>
    public IDictionary<string, string> GridServerEnvironmentVariables => O.GridServerEnvironmentVariables;

    /// <inheritdoc cref="IGridServerDockerSettings.HttpAccessKey"/>
    public string HttpAccessKey => O.HttpAccessKey;

    /// <inheritdoc cref="IGridServerDockerSettings.GridServerPrimaryDnsServer"/>
    public string GridServerPrimaryDnsServer => O.GridServerPrimaryDnsServer;

    /// <inheritdoc cref="IGridServerDockerSettings.GridServerSecondaryDnsServer"/>
    public string GridServerSecondaryDnsServer => O.GridServerSecondaryDnsServer;

    /// <inheritdoc cref="IGridServerDockerSettings.ContainerStopWaitBeforeKillInSeconds"/>
    public int ContainerStopWaitBeforeKillInSeconds => O.ContainerStopWaitBeforeKillInSeconds;

    /// <inheritdoc cref="IGridServerDockerSettings.MaxAttemptsToWaitForContainerExit"/>
    public int MaxAttemptsToWaitForContainerExit => O.MaxAttemptsToWaitForContainerExit;

    /// <inheritdoc cref="IJobManagerSettings.MaxInstanceReuses"/>
    public int MaxInstanceReuses => O.MaxInstanceReuses;

    /// <inheritdoc cref="IJobManagerSettings.MaxGridServerInstances"/>
    public int? MaxGridServerInstances => O.MaxGridServerInstances;

    /// <inheritdoc cref="IJobManagerSettings.PopulateReadyGridServerInstanceThreads"/>
    public int PopulateReadyGridServerInstanceThreads => O.PopulateReadyGridServerInstanceThreads;

    /// <inheritdoc cref="IJobManagerSettings.ReadyInstancesToKeepInReserve"/>
    public int ReadyInstancesToKeepInReserve => O.ReadyInstancesToKeepInReserve;

    /// <inheritdoc cref="IJobManagerSettings.GridServerStartAttempts"/>
    public int GridServerStartAttempts => O.GridServerStartAttempts;

    /// <inheritdoc cref="IJobManagerSettings.GridServerWaitForTcpSleepInterval"/>
    public TimeSpan GridServerWaitForTcpSleepInterval => O.GridServerWaitForTcpSleepInterval;

    /// <inheritdoc cref="IJobManagerSettings.GridServerSettingsApplicationName"/>
    public string GridServerSettingsApplicationName => O.GridServerSettingsApplicationName ?? "RCCService" + O.GridServerSettingsKey;

    /// <inheritdoc cref="IJobManagerSettings.GridServerSettingsBucketName"/>
    public string GridServerSettingsBucketName => O.GridServerSettingsBucketName;

    /// <inheritdoc cref="IJobManagerSettings.GridServerApplicationSettingsFilePath"/>
    public string GridServerApplicationSettingsFilePath =>
        O.GridServerApplicationSettingsFilePath ?? Path.Combine(O.GridServerSharedDirectoryAppData, O.GridServerApplicationSettingsFileName);

    /// <inheritdoc cref="IJobManagerSettings.GridServerApplicationSettingsValidWindow"/>
    public TimeSpan GridServerApplicationSettingsValidWindow => O.GridServerApplicationSettingsValidWindow;

    /// <inheritdoc cref="IJobManagerSettings.IsGridServerCpuAllocationCheckEnabled"/>
    public bool IsGridServerCpuAllocationCheckEnabled => O.IsGridServerCpuAllocationCheckEnabled;

    /// <inheritdoc cref="IJobManagerSettings.IsGridServerThreadsAllocationCheckEnabled"/>
    public bool IsGridServerThreadsAllocationCheckEnabled => O.IsGridServerThreadsAllocationCheckEnabled;

    /// <inheritdoc cref="IJobManagerSettings.IsGridServerMemoryAllocationCheckEnabled"/>
    public bool IsGridServerMemoryAllocationCheckEnabled => O.IsGridServerMemoryAllocationCheckEnabled;

    /// <inheritdoc cref="IJobManagerSettings.GridServerCpuOverAllocationRatio"/>
    public double GridServerCpuOverAllocationRatio => O.GridServerCpuOverAllocationRatio;

    /// <inheritdoc cref="IJobManagerSettings.GridServerThreadsOverAllocationRatio"/>
    public double GridServerThreadsOverAllocationRatio => O.GridServerThreadsOverAllocationRatio;

    /// <inheritdoc cref="IJobManagerSettings.GridServerMemoryOverAllocationRatio"/>
    public double GridServerMemoryOverAllocationRatio => O.GridServerMemoryOverAllocationRatio;

    /// <inheritdoc cref="IGridServerDockerSettings.MaxTimeToWaitForImage"/>
    public TimeSpan MaxTimeToWaitForImage => O.MaxTimeToWaitForImage;

    /// <inheritdoc cref="IGridServerDockerSettings.MaxTimeToWaitForInspectImage"/>
    public TimeSpan MaxTimeToWaitForInspectImage => O.MaxTimeToWaitForInspectImage;

    /// <inheritdoc cref="IGridServerProcessSettings.GridServerExecutableName"/>
    public string GridServerExecutableName => O.GridServerExecutableName;

    /// <inheritdoc cref="IGridServerProcessSettings.GridServerRegistryKeyName"/>
    public string GridServerRegistryKeyName => O.GridServerRegistryKeyName;

    /// <inheritdoc cref="IGridServerProcessSettings.GridServerRegistryValueName"/>
    public string GridServerRegistryValueName => O.GridServerRegistryValueName;

    /// <inheritdoc cref="IGridServerProcessSettings.VerboseLoggingEnabled"/>
    public bool VerboseLoggingEnabled => O.VerboseLoggingEnabled;

    /// <inheritdoc cref="IGridServerProcessSettings.GridServerApplicationSettingsFileName"/>
    public string GridServerApplicationSettingsFileName => O.GridServerApplicationSettingsFileName;
}
