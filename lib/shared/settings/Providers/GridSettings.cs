namespace Grid.Bot;

using System;
using System.IO;
using System.Collections.Generic;

using Logging;

/// <summary>
/// Settings provider for all arbiter related stuff.
/// </summary>
public class GridSettings : BaseSettingsProvider, IGridServerDockerSettings
{
    /// <inheritdoc cref="Configuration.IVaultProvider.Path"/>
    public override string Path => SettingsProvidersDefaults.GridPath;

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

    /// <inheritdoc cref="IGridServerDockerSettings.GridServerSharedDirectoryLogs"/>
    public TimeSpan MaxDelayBeforeFetchingNewGridServerContainer => GetOrDefault(
        nameof(MaxDelayBeforeFetchingNewGridServerContainer),
        TimeSpan.FromSeconds(10)
    );

    /// <inheritdoc cref="IGridServerDockerSettings.GridServerSharedDirectoryLogs"/>
    public string GridServerSharedDirectoryLogs => GetOrDefault(
        nameof(GridServerSharedDirectoryLogs),
        () => System.IO.Path.Combine(Directory.GetCurrentDirectory(), "logs")
    );

    /// <inheritdoc cref="IGridServerDockerSettings.GridServerSharedDirectoryInternalScripts"/>
    public string GridServerSharedDirectoryInternalScripts => GetOrDefault(
        nameof(GridServerSharedDirectoryInternalScripts),
        () => System.IO.Path.Combine(Directory.GetCurrentDirectory(), "internal-scripts")
    );

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

    /// <inheritdoc cref="IGridServerDockerSettings.GridServerMaxThreads"/>
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
    public string HttpAccessKey => GetOrDefault(
        nameof(HttpAccessKey),
        string.Empty
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
}
