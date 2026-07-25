#if GRID_SERVER_FOR_WINE

using Docker.DotNet.Models;

namespace Grid.ProcessManagement.Docker;

using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using Logging;

using Grid;
using Core;
using Grid.Commands;

/// <summary>
/// Represents the Wine Docker implementation of <see cref="GridServerInstanceBase"/>
/// </summary>
public sealed class GridServerDockerContainer : GridServerInstanceBase
{
    /// <summary>
    /// The port label.
    /// </summary>
    public const string PortLabel = "port";

    /// <summary>
    /// The image name label.
    /// </summary>
    public const string ImageNameLabel = "image_name";

    /// <summary>
    /// The Grid Server application name label.
    /// </summary>
    public const string GridServerApplicationNameLabel = "rcc_application_name";

    /// <summary>
    /// The Grid Server bucket name label.
    /// </summary>
    public const string GridServerBucketNameLabel = "rcc_bucket_name";

    /// <summary>
    /// The Grid Server Service version label.
    /// </summary>
    public const string GridServerVersionLabel = "rcc_version";

    /// <summary>
    /// The Grid Server appdata directory.
    /// </summary>
    public const string GridServerClientSettingsPath = "/opt/roblox/rcc_service";

    private const string _GridServerLogPath = "/opt/roblox/.wine/dosdevices/c:/users/root/AppData/Local/Roblox";
    private const string _X11SocketPath = "/tmp/.X11-unix";

    private const int _MillisecondToSecond = 1000;

    private readonly GridServerDockerAuthority _DockerAuthority;
    private readonly IGridServerDockerSettings _GridServerSettings;
    private readonly string _GridServerImageName;

    private bool _Disposed;

    /// <inheritdoc cref="GridServerInstanceBase.HasExited"/>
    public override bool HasExited => _DockerAuthority.HasContainerExited(ContainerName).Result;

    /// <inheritdoc cref="GridServerInstanceBase.Id"/>
    public override string Id => ContainerID;

    /// <inheritdoc cref="GridServerInstanceBase.Name"/>
    public override string Name => ContainerName;

    /// <summary>
    /// The name of the container.
    /// </summary>
    internal string ContainerName { get; set; }

    /// <summary>
    /// The ID of the container.
    /// </summary>
    internal string ContainerID { get; set; }

    /// <summary>
    /// Construct a new instance of <see cref="GridServerDockerContainer"/>
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="port">The port</param>
    /// <param name="version">The version.</param>
    /// <param name="gridServerSettings">The <see cref="IGridServerDockerSettings"/></param>
    /// <param name="applicationName">The Grid Server application name.</param>
    /// <param name="bucketName">The Grid Server bucket name.</param>
    /// <param name="dockerAuthority">The <see cref="GridServerDockerAuthority"/></param>
    /// <param name="gridServerImageName">The Grid Server Service image name.</param>
    /// <exception cref="ArgumentException"><paramref name="port"/> must be > 0</exception>
    internal GridServerDockerContainer(
        ILogger logger,
        int port,
        string version,
        IGridServerDockerSettings gridServerSettings,
        string applicationName,
        string bucketName,
        GridServerDockerAuthority dockerAuthority,
        string gridServerImageName
    )
        : base(logger, version, port, gridServerSettings, applicationName, bucketName)
    {
        if (port < 1) throw new ArgumentException("Port must be > 0", PortLabel);

        _GridServerSettings = gridServerSettings;
        _DockerAuthority = dockerAuthority;
        _GridServerImageName = gridServerImageName;

        ContainerName = string.Format("grid-server-{0}-gr", Guid.NewGuid());

        Logger.Information(
            "Constructing GridServerDockerContainer, ContainerName = {0}, Port = {1}, Version = {2}",
            ContainerName,
            Port,
            Version
        );
    }

    /// <inheritdoc cref="GridServerInstanceBase.Start"/>
    public override bool Start() => StartAsync().Result;

    private async Task<bool> StartAsync()
    {
        if (!await _DockerAuthority.CheckImageAsync(_GridServerImageName, Version).ConfigureAwait(false))
        {
            Logger.Information("Pulling container {0}:{1}", _GridServerImageName, Version);

            await _DockerAuthority.CreateImageAsync(_GridServerImageName, Version).ConfigureAwait(false);
        }

        ContainerID = await _DockerAuthority.CreateContainerAsync(GetCreateContainerParameters()).ConfigureAwait(false);

        Logger.Information("Created a new Container successfully with ID: {0}", ContainerID);

        if (string.IsNullOrEmpty(ContainerID))
        {
            Logger.Error(
                "Failed to Create Container, ContainerName = {0}, Version = {1}",
                ContainerName,
                Version
            );

            return false;
        }

        return await _DockerAuthority.StartContainerAsync(ContainerName).ConfigureAwait(false) && WaitForContainerStart();
    }

    private bool WaitForContainerStart()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            WaitForServiceToBecomeAvailable(false, sw);
            InitializeHighAvailability();

            return true;
        }
        catch (Exception ex)
        {
            var format = string.Format(
                "Error waiting for Grid Server Service Service to become available. Container Name: {0}, Version: {1}. Exception: {2}",
                ContainerName,
                Version,
                ex
            );

            Logger.Error(format);

            throw new Exception(format);
        }
    }

    private void InitializeHighAvailability()
    {
        using var soap = GetSoapInterface(60 * _MillisecondToSecond);

        var command = new ExecuteScriptCommand(
            new("highavailability", new Dictionary<string, object>())
        );
        var job = new Grid.Client.Job
        {
            id = Guid.NewGuid().ToString(),
            expirationInSeconds = 10000
        };

        soap.BatchJobEx(job, command);
    }

    private List<Mount> GetContainerMounts()
    {
        var additionalMounts = new List<Mount>();
        foreach (var mount in _GridServerSettings.GridServerAdditionalVolumeMappings)
            additionalMounts.Add(BuildMountFromString(mount));

        var mounts = new List<Mount>()
        {
            new()
            {
                Type = "bind",
                Source = GetSource(_GridServerSettings.GridServerSharedDirectoryLogs),
                ReadOnly = false,
                Target = _GridServerLogPath
            },
            new()
            {
                Type = "bind",
                Source = Path.Combine(_GridServerSettings.GridServerSharedDirectoryAppData, _GridServerSettings.GridServerApplicationSettingsFileName),
                ReadOnly = false,
                Target = $"{GridServerClientSettingsPath}/{_GridServerSettings.GridServerApplicationSettingsFileName}"
            },
            new()
            {
                Type = "bind",
                Source = _X11SocketPath,
                ReadOnly = true,
                Target = _X11SocketPath
            }
        };

        mounts.AddRange(additionalMounts);

        return mounts;

        string GetSource(string settingsValue) => string.IsNullOrEmpty(_GridServerSettings.MountPathOverride)
            ? settingsValue
            : _GridServerSettings.MountPathOverride;

        Mount BuildMountFromString(string mountString)
        {
            var parts = mountString.Split(':');
            if (parts.Length != 2)
                throw new Exception($"Invalid mount string: {mountString}. Expected format: source:target");

            var isReadOnly = parts.Length >= 3 && parts[2].Equals("ro", StringComparison.OrdinalIgnoreCase);
            
            return new Mount
            {
                Type = "bind",
                Source = parts[0],
                Target = parts[1],
                ReadOnly = isReadOnly
            };
        }
    }

    private List<string> GetEnvironmentVariables()
    {
        if (string.IsNullOrEmpty(_GridServerSettings.BaseUrl))
            throw new Exception("Unable to start a new Grid Server container, BaseUrl is set to null or is empty");

        var environmentVariables = new List<string>
        {
            $"RCC_PORT={Port}",
            $"RCC_HTTP_ACCESS_KEY={_GridServerSettings.HttpAccessKey}",
            $"BASE_URL={_GridServerSettings.BaseUrl}"
        };

        if (Environment.GetEnvironmentVariable("DISPLAY") != null)
            environmentVariables.Add($"DISPLAY={Environment.GetEnvironmentVariable("DISPLAY")}");

        if (_GridServerSettings.GridServerMaxThreads > 0)
            environmentVariables.Add($"MAXIMUM_THREADS={_GridServerSettings.GridServerMaxThreads}");

        int maxMemory = (int)(_GridServerSettings.GridServerMaxMemoryInBytes / 1048576);
        if (maxMemory > 0)
            environmentVariables.Add($"MAXIMUM_MEMORY={maxMemory}");

        if (!string.IsNullOrWhiteSpace(_GridServerSettings.GridServerSettingsKey))
            environmentVariables.Add($"RCC_SETTINGS_KEY={_GridServerSettings.GridServerSettingsKey}");

        if (_GridServerSettings.GridServerEnvironmentVariables != null)
            foreach (var environmentVariable in _GridServerSettings.GridServerEnvironmentVariables)
                environmentVariables.Add($"{environmentVariable.Key}={environmentVariable.Value}");

        return environmentVariables;
    }

    private CreateContainerParameters GetCreateContainerParameters()
    {
        if (string.IsNullOrEmpty(_GridServerSettings.HttpAccessKey))
            throw new Exception("Unable to start a new Grid Server Service container, HttpAccessKey is set to null or is empty");

        var containerParameters = new List<string>();

        if (!string.IsNullOrEmpty(_GridServerSettings.GridServerApplicationName))
            containerParameters.AddRange(new[] { "-ApplicationName", _GridServerSettings.GridServerApplicationName });

        if (!string.IsNullOrEmpty(_GridServerSettings.GridServerApplicationSettingsFileName))
        {
            Logger.Information("GetCreateContainerParameters. Adding --settingsFile command line option: {0}", _GridServerSettings.GridServerApplicationSettingsFileName);

            containerParameters.AddRange(new[] { "-SettingsFile", _GridServerSettings.GridServerApplicationSettingsFileName });
        }

        var parameters = new CreateContainerParameters
        {
            Image = $"{_GridServerImageName}:{Version}",
            Name = ContainerName,
            Cmd = containerParameters,
            Env = GetEnvironmentVariables()
        };

        var labels = new Dictionary<string, string>
        {
            [PortLabel] = Port.ToString(),
            [GridServerVersionLabel] = Version,
            [ImageNameLabel] = _GridServerImageName,
            [GridServerApplicationNameLabel] = ApplicationName,
            [GridServerBucketNameLabel] = BucketName
        };

        parameters.Labels = labels;
        parameters.HostConfig = new HostConfig
        {
            Mounts = GetContainerMounts(),
            Memory = _GridServerSettings.GridServerMaxMemoryInBytes,
            Ulimits = new List<Ulimit>
            {
                new()
                {
                    Name = "core",
                    Hard = 9999999999,
                    Soft = 9999999999
                },
                new()
                {
                    Name = "nofile",
                    Hard = 8192,
                    Soft = 4096
                }
            },
            NetworkMode = "host"
        };

        if (_GridServerSettings.ReservedCoresPerGridServerInstance != null)
        {
            parameters.HostConfig.CPUPeriod = 100000;
            parameters.HostConfig.CPUQuota = _DockerAuthority.CalculateCpuQuota(_GridServerSettings.ReservedCoresPerGridServerInstance.Value, 100000);
        }

        if (!string.IsNullOrEmpty(_GridServerSettings.GridServerPrimaryDnsServer))
        {
            var dnsConfiguration = new List<string> { _GridServerSettings.GridServerPrimaryDnsServer };

            if (!string.IsNullOrEmpty(_GridServerSettings.GridServerSecondaryDnsServer))
                dnsConfiguration.Add(_GridServerSettings.GridServerSecondaryDnsServer);

            parameters.HostConfig.DNS = dnsConfiguration;
        }

        return parameters;
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public override void Dispose()
    {
        if (_Disposed) return;

        _DockerAuthority.KillContainerAsync(ContainerID).Wait();
        _DockerAuthority.RemoveContainerWithRetriesAsync(ContainerID).Wait();

        _Disposed = true;
    }
}

#endif
