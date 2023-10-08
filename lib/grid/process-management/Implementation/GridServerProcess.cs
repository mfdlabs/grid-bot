namespace Grid;

using System;
using System.Diagnostics;

using Logging;

/// <summary>
/// Represents the Docker implementation of <see cref="GridServerInstanceBase"/>
/// </summary>
public sealed class GridServerProcess : GridServerInstanceBase
{
    private readonly IGridServerProcess _Process;
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
    /// <param name="gridServerProcess">The <see cref="IGridServerProcess"/></param>
    /// <param name="fileHelper">The <see cref="IGridServerFileHelper"/></param>
    /// <exception cref="ArgumentException"><paramref name="port"/> must be > 0</exception>
    internal GridServerProcess(
        ILogger logger,
        int port,
        string version,
        IGridServerProcessSettings gridServerSettings,
        IGridServerProcess gridServerProcess,
        IGridServerFileHelper fileHelper = null
    )
        : base(logger, version, port, gridServerSettings)
    {
        if (port < 1) throw new ArgumentException("Port must be > 0", nameof(port));

        _GridServerSettings = gridServerSettings;
        _Process = gridServerProcess;

        ProcessName = string.Format("grid-server-{0}-gr", Guid.NewGuid());

        _FileHelper = fileHelper ?? new GridServerFileHelper(gridServerSettings);

        Logger.Information("Constructing GridServerProcess",
            new
            {
                ProcessName,
                Port,
                Version
            }
        );
    }

    /// <inheritdoc cref="GridServerInstanceBase.Start"/>
    public override bool Start() 
        => _Process.Start(_GridServerSettings.GridServerExecutableName, _FileHelper.GetGridServerPath(true), Port) && WaitForProcessStart();

    private bool WaitForProcessStart()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            WaitForServiceToBecomeAvailable(false, sw);

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

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public override void Dispose()
    {
        if (_Disposed) return;

        _Process.Kill();
        _Process.Dispose();

        _Disposed = true;
    }
}
