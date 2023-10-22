namespace Grid;

using System;
using System.IO;
using System.Net;
using System.Diagnostics;

using Logging;

/// <inheritdoc cref="IGridServerProcess"/>
[DebuggerDisplay($"{{{nameof(ToString)}(), nq}}")]
internal class RawGridServerProcess : IGridServerProcess, IDisposable
{
    private Process _process;
    private IPEndPoint _endpoint;

    private ILogger _logger;
    private bool _disposed;

    /// <summary>
    /// Construct a new instance of <see cref="RawGridServerProcess"/>
    /// </summary>
    public RawGridServerProcess() {}

    /// <summary>
    /// Construct a new instance of <see cref="RawGridServerProcess"/>
    /// </summary>
    /// <param name="process">The <see cref="Process"/></param>
    /// <param name="port">The port.</param>
    /// <exception cref="ArgumentNullException"><paramref name="process"/> cannot be null.</exception>
    public RawGridServerProcess(Process process, int port)
    {
        _process = process ?? throw new ArgumentNullException(nameof(process));
        _endpoint = new IPEndPoint(IPAddress.Loopback, port);
    }

    /// <summary>
    /// Construct a new instance of <see cref="RawGridServerProcess"/>
    /// </summary>
    /// <param name="process">The <see cref="Process"/></param>
    /// <param name="endPoint">The <see cref="IPEndPoint"/></param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="process"/> cannot be null.
    /// - <paramref name="endPoint"/> cannot be null.
    /// </exception>
    public RawGridServerProcess(Process process, IPEndPoint endPoint)
    {
        _process = process ?? throw new ArgumentNullException(nameof(process));
        _endpoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
    }

    /// <inheritdoc cref="IGridServerProcess.RawProcess"/>
    public Process RawProcess => _process;

    /// <inheritdoc cref="IGridServerProcess.HasExited"/>
    public bool HasExited => _disposed || _process.SafeGetHasExited();

    /// <inheritdoc cref="IGridServerProcess.EndPoint"/>
    public IPEndPoint EndPoint => _endpoint;

    /// <inheritdoc cref="IGridServerProcess.IsDisposed"/>
    public bool IsDisposed => _disposed;

    /// <inheritdoc cref="IGridServerProcess.SetLogger(ILogger)"/>
    public void SetLogger(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc cref="IGridServerProcess.Kill"/>
    public void Kill()
    {
        var (didClose, errorCode) = _process.ForceKill();

        _logger?.Debug("Process '{0}' did close ({1}) with error code {2}", _process.Id, didClose, errorCode);
    }

    /// <inheritdoc cref="Process.ToString"/>
    public override string ToString() => $"[{_process.ProcessName} ({_process.Id}) @ {_endpoint}]";

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose()
    {
        if (_disposed) return;

        GC.SuppressFinalize(this);

        _process?.Dispose();
        _process = null;

        _disposed = true;
    }

    /// <inheritdoc cref="IGridServerProcess.Start(string, string, int, string)"/>
    public bool Start(
        string executableName,
        string workingDirectory = null,
        int port = 53640,
        string args = null
    )
    {
        if (!HasExited) return true;

        if (TcpHealthCheck.GetProcessByHostnameAndPort(IPAddress.Loopback.ToString(), port, out var proc))
        {
            _endpoint = new IPEndPoint(IPAddress.Loopback, port);
            _process = proc;

            return true;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = !string.IsNullOrEmpty(workingDirectory) 
                ? Path.Combine(workingDirectory, executableName)
                : executableName,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (!string.IsNullOrEmpty(workingDirectory))
            startInfo.WorkingDirectory = workingDirectory;
        else
            startInfo.WorkingDirectory = Directory.GetCurrentDirectory();

        if (!string.IsNullOrEmpty(args))
            startInfo.Arguments = args;
        else
            startInfo.Arguments = $"{port} -Console";

        _endpoint = new IPEndPoint(IPAddress.Loopback, port);
        _process = Process.Start(startInfo);

        return !_process.HasExited;
    }
}
