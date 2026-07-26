namespace Grid.Bot;

using System;
using System.IO;
using System.Collections.Generic;

using Logging;

using ProcessManagement;
using ProcessManagement.Core;
using ProcessManagement.Docker;

/// <summary>
/// Settings provider for all arbiter related stuff.
/// </summary>
public class GridSettings : BaseSettingsProvider, IGridServerDockerSettings, IGridServerProcessSettings
{
    /// <inheritdoc cref="Configuration.IVaultProvider.Path"/>
    public override string Path => SettingsProvidersDefaults.GridPath;

#if DEBUG

    /// <summary>
    /// Determines if the grid should resort to NOOP system for debugging on systems not on the infra.
    /// </summary>
    public bool DebugUseNoopJobManager => GetOrDefault(nameof(DebugUseNoopJobManager), false);

#endif

    /// <summary>
    /// Gets the name of the job manager logger.
    /// </summary>
    public string JobManagerLoggerName => GetOrDefault(
        nameof(JobManagerLoggerName),
        "job-manager"
    );

    /// <summary>
    /// Gets the log level for the job manager.
    /// </summary>
    public LogLevel JobManagerLogLevel => GetOrDefault(
        nameof(JobManagerLogLevel),
        LogLevel.Information
    );

    /// <summary>
    /// Should the job manager log to console?
    /// </summary>
    public bool JobManagerLogToConsole => GetOrDefault(
        nameof(JobManagerLogToConsole),
        true
    );

    /// <summary>
    /// Gets the timeout for the script execution grid-server arbiter.
    /// </summary>
    public TimeSpan ScriptExecutionJobMaxTimeout => GetOrDefault(
        nameof(ScriptExecutionJobMaxTimeout),
        TimeSpan.FromSeconds(15)
    );

    /// <inheritdoc cref="IGridServerDockerSettings.GridServerImageName"/>
    public string GridServerImageName => GetOrDefault<string>(
        nameof(GridServerImageName),
        () => throw new InvalidOperationException($"Missing required configuration value '{nameof(GridServerImageName)}'")
    );

    /// <inheritdoc cref="IGridServerDockerSettings.GridServerImageTag"/>
    public string GridServerImageTag => GetOrDefault<string>(
        nameof(GridServerImageTag),
        () => throw new InvalidOperationException($"Missing required configuration value '{nameof(GridServerImageTag)}'")
    );

    /// <inheritdoc cref="IGridServerDockerSettings.GridServerSettingsKey"/>
    public string GridServerSettingsKey => GetOrDefault<string>(
        nameof(GridServerSettingsKey),
        () => throw new InvalidOperationException($"Missing required configuration value '{nameof(GridServerSettingsKey)}'")
    );

    /// <inheritdoc cref="IGridServerDockerSettings.DockerRegistryUsername"/>
    public string DockerRegistryUsername => GetOrDefault(
        nameof(DockerRegistryUsername),
        string.Empty
    );

    /// <inheritdoc cref="IGridServerDockerSettings.DockerRegistryPassword"/>
    public string DockerRegistryPassword => GetOrDefault(
        nameof(DockerRegistryPassword),
        string.Empty
    );

    /// <inheritdoc cref="IGridServerDockerSettings.DockerRegistryIdentityToken"/>
    public string DockerRegistryIdentityToken => GetOrDefault(
        nameof(DockerRegistryIdentityToken),
        string.Empty
    );

    /// <inheritdoc cref="IGridServerDockerSettings.IsRemoveVolumesEnabled"/>
    public bool? IsRemoveVolumesEnabled => GetOrDefault(
        nameof(IsRemoveVolumesEnabled),
        true
    );

    /// <inheritdoc cref="IGridServerDockerSettings.ContainerStopSleepIntervalMilliseconds"/>
    public int? ContainerStopSleepIntervalMilliseconds => GetOrDefault(
        nameof(ContainerStopSleepIntervalMilliseconds),
        1000
    );

    /// <inheritdoc cref="IGridServerDockerSettings.MountPathOverride"/>
    public string MountPathOverride => GetOrDefault(
        nameof(MountPathOverride),
        string.Empty
    );

    /// <inheritdoc cref="IGridServerDockerSettings.MaxDelayBeforeFetchingNewGridServerContainer"/>
    public TimeSpan MaxDelayBeforeFetchingNewGridServerContainer => GetOrDefault(
        nameof(MaxDelayBeforeFetchingNewGridServerContainer),
        TimeSpan.FromSeconds(10)
    );

    /// <inheritdoc cref="IGridServerDockerSettings.GridServerSharedDirectoryLogs"/>
    public string GridServerSharedDirectoryLogs => GetOrDefault(
        nameof(GridServerSharedDirectoryLogs),
        () => System.IO.Path.Combine(Directory.GetCurrentDirectory(), "logs")
    );

    /// <summary>
    /// Gets the directory where shared Grid Server Service internal scripts are stored.
    /// </summary>
    /// <remarks>
    /// Originally, this was built into process-management-docker,
    /// but was removed because it is technically not feasible
    /// </remarks>
    public string GridServerSharedDirectoryInternalScripts => GetOrDefault(
        nameof(GridServerSharedDirectoryInternalScripts),
        () => System.IO.Path.Combine(Directory.GetCurrentDirectory(), "internal-scripts")
    );

    /// <summary>
    /// Gets the directory where shared Grid Server Service internal scripts are stored inside the container.
    /// </summary>
    public string GridServerInsideDirectoryInternalScripts => GetOrDefault(
        nameof(GridServerInsideDirectoryInternalScripts),
        "/opt/roblox/rcc_service/internalscripts"
    );

    /// <inheritdoc cref="IGridServerDockerSettings.BaseUrl"/>
    public string BaseUrl => GetOrDefault(
        nameof(BaseUrl),
        "http://www.sitetest4.robloxlabs.com"
    );

    /// <inheritdoc cref="IGridServerDockerSettings.GridServerSharedDirectoryAppData"/>
    public string GridServerSharedDirectoryAppData => GetOrDefault(
        nameof(GridServerSharedDirectoryAppData),
        () => System.IO.Path.Combine(Directory.GetCurrentDirectory(), "app-data")
    );

    /// <inheritdoc cref="IGridServerDockerSettings.GridServerAdditionalVolumeMappings"/>
    public string[] GridServerAdditionalVolumeMappingsSetting  => GetOrDefault(
        nameof(GridServerAdditionalVolumeMappings),
        Array.Empty<string>
    );

    /// <inheritdoc cref="IGridServerDockerSettings.GridServerAdditionalVolumeMappings"/>
    public string[] GridServerAdditionalVolumeMappings { get; set; }

    /// <inheritdoc cref="IGridServerDockerSettings.ReservedCoresPerGridServerInstance"/>
    public int? ReservedCoresPerGridServerInstance => GetOrDefault<int?>(
        nameof(ReservedCoresPerGridServerInstance),
        null as int?
    );

    /// <inheritdoc cref="IGridServerDockerSettings.GridServerMaxMemoryInBytes"/>
    public long GridServerMaxMemoryInBytes => GetOrDefault(
        nameof(GridServerMaxMemoryInBytes),
        500 * 1024 * 1024
    );

    /// <inheritdoc cref="IJobManagerSettings.GridServerMaxThreads"/>
    public int GridServerMaxThreads => GetOrDefault(
        nameof(GridServerMaxThreads),
        0
    );

    /// <inheritdoc cref="IGridServerDockerSettings.GridServerEnvironmentVariables"/>
    public IDictionary<string, string> GridServerEnvironmentVariables => GetOrDefault(
        nameof(GridServerEnvironmentVariables),
        null as IDictionary<string, string>
    );

    /// <inheritdoc cref="IGridServerDockerSettings.HttpAccessKey"/>
    public string HttpAccessKey => GetOrDefault<string>(
        nameof(HttpAccessKey),
        Guid.NewGuid().ToString() // Appeasing the original thing where all I need is the tags
    );

    /// <inheritdoc cref="IGridServerDockerSettings.GridServerPrimaryDnsServer"/>
    public string GridServerPrimaryDnsServer => GetOrDefault(
        nameof(GridServerPrimaryDnsServer),
        string.Empty
    );

    /// <inheritdoc cref="IGridServerDockerSettings.GridServerSecondaryDnsServer"/>
    public string GridServerSecondaryDnsServer => GetOrDefault(
        nameof(GridServerSecondaryDnsServer),
        string.Empty
    );

    /// <inheritdoc cref="IGridServerDockerSettings.ContainerStopWaitBeforeKillInSeconds"/>
    public int ContainerStopWaitBeforeKillInSeconds => GetOrDefault(
        nameof(ContainerStopWaitBeforeKillInSeconds),
        0
    );

    /// <inheritdoc cref="IGridServerDockerSettings.MaxAttemptsToWaitForContainerExit"/>
    public int MaxAttemptsToWaitForContainerExit => GetOrDefault(
        nameof(MaxAttemptsToWaitForContainerExit),
        5
    );

    /// <inheritdoc cref="IJobManagerSettings.MaxInstanceReuses"/>
    public int MaxInstanceReuses => GetOrDefault(
        nameof(MaxInstanceReuses),
        1
    );

    /// <inheritdoc cref="IJobManagerSettings.MaxGridServerInstances"/>
    public int? MaxGridServerInstances => GetOrDefault(
        nameof(MaxGridServerInstances),
        null as int?
    );

    /// <inheritdoc cref="IJobManagerSettings.PopulateReadyGridServerInstanceThreads"/>
    public int PopulateReadyGridServerInstanceThreads => GetOrDefault(
        nameof(PopulateReadyGridServerInstanceThreads),
        2
    );

    /// <inheritdoc cref="IJobManagerSettings.ReadyInstancesToKeepInReserve"/>
    public int ReadyInstancesToKeepInReserve => GetOrDefault(
        nameof(ReadyInstancesToKeepInReserve),
        5
    );

    /// <inheritdoc cref="IJobManagerSettings.GridServerStartAttempts"/>
    public int GridServerStartAttempts => GetOrDefault(
        nameof(GridServerStartAttempts),
        10
    );

    /// <inheritdoc cref="IJobManagerSettings.GridServerWaitForTcpSleepInterval"/>
    public TimeSpan GridServerWaitForTcpSleepInterval => GetOrDefault(
        nameof(GridServerWaitForTcpSleepInterval),
        TimeSpan.FromSeconds(5)
    );


    /// <inheritdoc cref="IJobManagerSettings.GridServerSettingsApplicationName"/>
    public string GridServerSettingsApplicationName => GetOrDefault(
        nameof(GridServerSettingsApplicationName),
        () => "RCCService" + GridServerSettingsKey
    );

    /// <inheritdoc cref="IJobManagerSettings.GridServerSettingsBucketName"/>
    public string GridServerSettingsBucketName => GetOrDefault(
        nameof(GridServerSettingsBucketName),
        string.Empty
    );

    /// <inheritdoc cref="IJobManagerSettings.GridServerApplicationSettingsFilePath"/>
    public string GridServerApplicationSettingsFilePath => GetOrDefault(
        nameof(GridServerApplicationSettingsFilePath),
        () => System.IO.Path.Combine(GridServerSharedDirectoryAppData, GridServerApplicationSettingsFileName)
    );

    /// <inheritdoc cref="IJobManagerSettings.GridServerApplicationSettingsValidWindow"/>
    public TimeSpan GridServerApplicationSettingsValidWindow => GetOrDefault(
        nameof(GridServerApplicationSettingsValidWindow),
        TimeSpan.FromHours(1)
    );

    /// <inheritdoc cref="IJobManagerSettings.IsGridServerCpuAllocationCheckEnabled"/>
    public bool IsGridServerCpuAllocationCheckEnabled => GetOrDefault(
        nameof(IsGridServerCpuAllocationCheckEnabled),
        false
    );

    /// <inheritdoc cref="IJobManagerSettings.IsGridServerThreadsAllocationCheckEnabled"/>
    public bool IsGridServerThreadsAllocationCheckEnabled => GetOrDefault(
        nameof(IsGridServerThreadsAllocationCheckEnabled),
        false
    );

    /// <inheritdoc cref="IJobManagerSettings.IsGridServerMemoryAllocationCheckEnabled"/>
    public bool IsGridServerMemoryAllocationCheckEnabled => GetOrDefault(
        nameof(IsGridServerMemoryAllocationCheckEnabled),
        false
    );

    /// <inheritdoc cref="IJobManagerSettings.GridServerCpuOverAllocationRatio"/>
    public double GridServerCpuOverAllocationRatio => GetOrDefault(
        nameof(GridServerCpuOverAllocationRatio),
        1
    );

    /// <inheritdoc cref="IJobManagerSettings.GridServerThreadsOverAllocationRatio"/>
    public double GridServerThreadsOverAllocationRatio => GetOrDefault(
        nameof(GridServerThreadsOverAllocationRatio),
        1
    );

    /// <inheritdoc cref="IJobManagerSettings.GridServerMemoryOverAllocationRatio"/>
    public double GridServerMemoryOverAllocationRatio => GetOrDefault(
        nameof(GridServerMemoryOverAllocationRatio),
        1
    );

    /// <inheritdoc cref="IGridServerDockerSettings.MaxTimeToWaitForImage"/>
    public TimeSpan MaxTimeToWaitForImage => GetOrDefault(
        nameof(MaxTimeToWaitForImage),
        TimeSpan.FromMinutes(10)
    );

    /// <inheritdoc cref="IGridServerDockerSettings.MaxTimeToWaitForInspectImage"/>
    public TimeSpan MaxTimeToWaitForInspectImage => GetOrDefault(
        nameof(MaxTimeToWaitForInspectImage),
        TimeSpan.FromSeconds(5)
    );

    /// <inheritdoc cref="IGridServerProcessSettings.GridServerExecutableName"/> 
    public string GridServerExecutableName => GetOrDefault(
        nameof(GridServerExecutableName),
        "gridserver.exe"
    );

    /// <inheritdoc cref="IGridServerProcessSettings.GridServerExecutableName"/> 
    public string GridServerRegistryKeyName => GetOrDefault<string>(
        nameof(GridServerRegistryKeyName),
        () => throw new InvalidOperationException($"Missing required configuration value '{nameof(GridServerRegistryKeyName)}")
    );

    /// <inheritdoc cref="IGridServerProcessSettings.GridServerExecutableName"/> 
    public string GridServerRegistryValueName => GetOrDefault<string>(
        nameof(GridServerRegistryValueName),
        () => throw new InvalidOperationException($"Missing required configuration value '{nameof(GridServerRegistryValueName)}")
    );

    /// <inheritdoc cref="IGridServerProcessSettings.VerboseLoggingEnabled"/>
    public bool VerboseLoggingEnabled => GetOrDefault(
        nameof(VerboseLoggingEnabled),
        false
    );

    /// <inheritdoc cref="IGridServerProcessSettings.GridServerApplicationSettingsFileName"/>
    public string GridServerApplicationSettingsFileName => GetOrDefault<string>(
        nameof(GridServerApplicationSettingsFileName),
        "grid-server-settings.json"
    );
}
