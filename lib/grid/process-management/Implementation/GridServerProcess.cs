namespace Grid.ProcessManagement;

using System;
using System.Diagnostics;
using System.Collections.Generic;

using Logging;
using Commands;

using Core;

/// <summary>
/// Represents the Docker implementation of <see cref="GridServerInstanceBase"/>
/// </summary>
public sealed class GridServerProcess : GridServerInstanceBase
{
    private const int _MillisecondToSecond = 1000;

    private readonly IRawGridServerProcess _Process;
    private readonly IGridServerProcessSettings _GridServerSettings;
    private readonly IGridServerFileHelper _FileHelper;

    private bool _Disposed;

    /// <inheritdoc cref="GridServerInstanceBase.HasExited"/>
    public override bool HasExited => _Process.HasExited;

    /// <inheritdoc cref="GridServerInstanceBase.Id"/>
    public override string Id => _Process.RawProcess.Id.ToString();

    /// <inheritdoc cref="GridServerInstanceBase.Name"/>
    public override string Name => ProcessName;

    /// <summary>
    /// The name of the container.
    /// </summary>
    internal string ProcessName { get; set; }

    /// <summary>
    /// Construct a new instance of <see cref="GridServerProcess"/>
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="port">The port</param>
    /// <param name="version">The version.</param>
    /// <param name="gridServerSettings">The <see cref="IGridServerProcessSettings"/></param>
    /// <param name="applicationName">The GridServer application name.</param>
    /// <param name="bucketName">The GridServer bucket name.</param>
    /// <param name="gridServerProcess">The <see cref="IRawGridServerProcess"/></param>
    /// <param name="fileHelper">The <see cref="IGridServerFileHelper"/></param>
    /// <exception cref="ArgumentException"><paramref name="port"/> must be > 0</exception>
    internal GridServerProcess(
        ILogger logger,
        int port,
        string version,
        IGridServerProcessSettings gridServerSettings,
        string applicationName,
        string bucketName,
        IRawGridServerProcess gridServerProcess,
        IGridServerFileHelper fileHelper = null
    )
        : base(logger, version, port, gridServerSettings, applicationName, bucketName)
    {
        if (port < 1) throw new ArgumentException("Port must be > 0", nameof(port));

        _GridServerSettings = gridServerSettings;
        _Process = gridServerProcess;

        ProcessName = string.Format("grid-server-{0}-gr", Guid.NewGuid());

        _FileHelper = fileHelper ?? new GridServerFileHelper(gridServerSettings);

        Logger.Information(
            "Constructing GridServerProcess, ProcessName = {0}, Port = {1}, Version = {2}",
            ProcessName,
            Port,
            Version
        );
    }

    /// <inheritdoc cref="GridServerInstanceBase.Start"/>
    public override bool Start()
        => _Process.Start(_GridServerSettings.GridServerExecutableName, _FileHelper.GetGridServerPath(), Port,  _GridServerSettings.GridServerMaxThreads, _GridServerSettings.GridServerMaxMemoryInBytes, GetArguments()) && WaitForProcessStart();

    private string GetArguments()
    {
        var arguments = new List<string>()
        {
            "-Console"
        };

        if (_GridServerSettings.VerboseLoggingEnabled)
            arguments.Add("-Verbose");

        if (!string.IsNullOrEmpty(_GridServerSettings.GridServerSettingsApplicationName))
            arguments.AddRange(new[] { "-ApplicationName", _GridServerSettings.GridServerSettingsApplicationName });

        if (!string.IsNullOrEmpty(_GridServerSettings.GridServerApplicationSettingsFileName))
        {
            Logger.Information("GetArguments. Adding -SettingsFile command line option: {0}", _GridServerSettings.GridServerApplicationSettingsFileName);

            arguments.AddRange(new[] { "-SettingsFile", _GridServerSettings.GridServerApplicationSettingsFileName });
        }

        arguments.Add(Port.ToString());

        return string.Join(" ", arguments);
    }

    private bool WaitForProcessStart()
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
                "Error waiting for Grid Server Service to become available. Process Name: {0}, Version: {1}. Exception: {2}",
                ProcessName,
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

#if !PRE_JSON_EXECUTION
        var command = new ExecuteScriptCommand(
            new("highavailability", new Dictionary<string, object>())
        );
        var job = new Client.Job
        {
            id = Guid.NewGuid().ToString(),
            expirationInSeconds = 10000
        };

        soap.BatchJobEx(job, command);

#else
        var lua = ScriptProvider.GetScript("HighAvailability");

        var job = new Client.Job
        {
            id = Guid.NewGuid().ToString(),
            expirationInSeconds = 10000
        };

        soap.BatchJobEx(job, lua);
#endif
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public override void Dispose()
    {
        if (_Disposed) return;

        _Process.Kill();
        _Process.Dispose();

        _Disposed = true;
    }
}
