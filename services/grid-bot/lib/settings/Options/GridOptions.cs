namespace Grid.Bot;

using System;
using System.IO;
using System.Collections.Generic;

using Logging;

/// <summary>
/// Options for the grid server and job manager.
/// </summary>
public class GridOptions
{
    private static readonly string _defaultHttpAccessKey = Guid.NewGuid().ToString();

    /// <summary>
    /// The configuration section these options bind from.
    /// </summary>
    public const string SectionName = "Grid";

    /// <summary>
    /// The vault path for the grid options.
    /// </summary>
    public const string VaultPath = "grid";

#if DEBUG
    /// <summary>
    /// Gets or sets a value indicating whether the grid resorts to a NOOP job manager for debugging on systems not on the infra.
    /// </summary>
    public bool DebugUseNoopJobManager { get; set; }
#endif

    /// <summary>
    /// Gets or sets the name of the job manager logger.
    /// </summary>
    public string JobManagerLoggerName { get; set; } = "job-manager";

    /// <summary>
    /// Gets or sets the log level for the job manager.
    /// </summary>
    public LogLevel JobManagerLogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets or sets a value indicating whether the job manager logs to console.
    /// </summary>
    public bool JobManagerLogToConsole { get; set; } = true;

    /// <summary>
    /// Gets or sets the timeout for the script execution grid-server arbiter.
    /// </summary>
    public TimeSpan ScriptExecutionJobMaxTimeout { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Gets or sets the grid server image name. Required when using Docker.
    /// </summary>
    public string GridServerImageName { get; set; }

    /// <summary>
    /// Gets or sets the grid server image tag. Required when using Docker.
    /// </summary>
    public string GridServerImageTag { get; set; }

    /// <summary>
    /// Gets or sets the grid server settings key. Required when using Docker.
    /// </summary>
    public string GridServerSettingsKey { get; set; }

    /// <summary>
    /// Gets or sets the Docker registry username.
    /// </summary>
    public string DockerRegistryUsername { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Docker registry password.
    /// </summary>
    public string DockerRegistryPassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Docker registry identity token.
    /// </summary>
    public string DockerRegistryIdentityToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether volumes are removed with containers.
    /// </summary>
    public bool? IsRemoveVolumesEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the container stop sleep interval in milliseconds.
    /// </summary>
    public int? ContainerStopSleepIntervalMilliseconds { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the mount path override.
    /// </summary>
    public string MountPathOverride { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum delay before fetching a new grid server container.
    /// </summary>
    public TimeSpan MaxDelayBeforeFetchingNewGridServerContainer { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the shared directory for grid server logs.
    /// </summary>
    public string GridServerSharedDirectoryLogs { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "logs");

    /// <summary>
    /// Gets or sets the directory where shared Grid Server Service internal scripts are stored.
    /// </summary>
    public string GridServerSharedDirectoryInternalScripts { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "internal-scripts");

    /// <summary>
    /// Gets or sets the directory where Grid Server Service internal scripts are stored inside the container.
    /// </summary>
    public string GridServerInsideDirectoryInternalScripts { get; set; } = "/opt/roblox/rcc_service/internalscripts";

    /// <summary>
    /// Gets or sets the base URL.
    /// </summary>
    public string BaseUrl { get; set; } = "http://www.sitetest4.robloxlabs.com";

    /// <summary>
    /// Gets or sets the shared directory for grid server app data.
    /// </summary>
    public string GridServerSharedDirectoryAppData { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "app-data");

    /// <summary>
    /// Gets or sets additional volume mappings. The internal scripts mapping is always appended.
    /// </summary>
    public string[] GridServerAdditionalVolumeMappings { get; set; } = [];

    /// <summary>
    /// Gets or sets the reserved cores per grid server instance.
    /// </summary>
    public int? ReservedCoresPerGridServerInstance { get; set; }

    /// <summary>
    /// Gets or sets the maximum memory in bytes per grid server.
    /// </summary>
    public long GridServerMaxMemoryInBytes { get; set; } = 500 * 1024 * 1024;

    /// <summary>
    /// Gets or sets the maximum threads per grid server.
    /// </summary>
    public int GridServerMaxThreads { get; set; }

    /// <summary>
    /// Gets or sets the environment variables for the grid server.
    /// </summary>
    public IDictionary<string, string> GridServerEnvironmentVariables { get; set; }

    /// <summary>
    /// Gets or sets the HTTP access key.
    /// </summary>
    public string HttpAccessKey { get; set; } = _defaultHttpAccessKey;

    /// <summary>
    /// Gets or sets the primary DNS server for grid servers.
    /// </summary>
    public string GridServerPrimaryDnsServer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the secondary DNS server for grid servers.
    /// </summary>
    public string GridServerSecondaryDnsServer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the seconds to wait before killing a stopping container.
    /// </summary>
    public int ContainerStopWaitBeforeKillInSeconds { get; set; }

    /// <summary>
    /// Gets or sets the maximum attempts to wait for a container to exit.
    /// </summary>
    public int MaxAttemptsToWaitForContainerExit { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum amount of times an instance can be reused.
    /// </summary>
    public int MaxInstanceReuses { get; set; } = 1;

    /// <summary>
    /// Gets or sets the maximum amount of grid server instances.
    /// </summary>
    public int? MaxGridServerInstances { get; set; }

    /// <summary>
    /// Gets or sets the amount of threads used to populate the ready instance pool.
    /// </summary>
    public int PopulateReadyGridServerInstanceThreads { get; set; } = 2;

    /// <summary>
    /// Gets or sets the amount of ready instances to keep in reserve.
    /// </summary>
    public int ReadyInstancesToKeepInReserve { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum start attempts for a grid server instance.
    /// </summary>
    public int GridServerStartAttempts { get; set; } = 10;

    /// <summary>
    /// Gets or sets the sleep interval while waiting for the grid server TCP port.
    /// </summary>
    public TimeSpan GridServerWaitForTcpSleepInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the grid server application settings name, defaulting to RCCService plus the settings key.
    /// </summary>
    public string GridServerSettingsApplicationName { get; set; }

    /// <summary>
    /// Gets or sets the grid server application settings bucket name.
    /// </summary>
    public string GridServerSettingsBucketName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the grid server application settings file path, defaulting to the app data directory plus the file name.
    /// </summary>
    public string GridServerApplicationSettingsFilePath { get; set; }

    /// <summary>
    /// Gets or sets the valid window in which to update application settings.
    /// </summary>
    public TimeSpan GridServerApplicationSettingsValidWindow { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets or sets a value indicating whether the CPU allocation check is enabled.
    /// </summary>
    public bool IsGridServerCpuAllocationCheckEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the threads allocation check is enabled.
    /// </summary>
    public bool IsGridServerThreadsAllocationCheckEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the memory allocation check is enabled.
    /// </summary>
    public bool IsGridServerMemoryAllocationCheckEnabled { get; set; }

    /// <summary>
    /// Gets or sets the CPU over allocation ratio.
    /// </summary>
    public double GridServerCpuOverAllocationRatio { get; set; } = 1;

    /// <summary>
    /// Gets or sets the threads over allocation ratio.
    /// </summary>
    public double GridServerThreadsOverAllocationRatio { get; set; } = 1;

    /// <summary>
    /// Gets or sets the memory over allocation ratio.
    /// </summary>
    public double GridServerMemoryOverAllocationRatio { get; set; } = 1;

    /// <summary>
    /// Gets or sets the maximum time to wait for an image.
    /// </summary>
    public TimeSpan MaxTimeToWaitForImage { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Gets or sets the maximum time to wait for an image inspection.
    /// </summary>
    public TimeSpan MaxTimeToWaitForInspectImage { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the grid server executable name.
    /// </summary>
    public string GridServerExecutableName { get; set; } = "gridserver.exe";

    /// <summary>
    /// Gets or sets the grid server registry key name. Required on Windows.
    /// </summary>
    public string GridServerRegistryKeyName { get; set; }

    /// <summary>
    /// Gets or sets the grid server registry value name. Required on Windows.
    /// </summary>
    public string GridServerRegistryValueName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether verbose logging is enabled.
    /// </summary>
    public bool VerboseLoggingEnabled { get; set; }

    /// <summary>
    /// Gets or sets the grid server application settings file name.
    /// </summary>
    public string GridServerApplicationSettingsFileName { get; set; } = "grid-server-settings.json";
}
