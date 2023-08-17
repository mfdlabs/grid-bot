namespace Grid;

using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

using Logging;

#nullable enable

/// <inheritdoc cref="IGridServerProcess"/>
[DebuggerDisplay($"{{{nameof(ToString)}(), nq}}")]
public class GridServerProcess : IGridServerProcess, IDisposable
{
    private readonly Process _process;
    private readonly IPEndPoint _endpoint;

    private ILogger? _logger;
    private bool _disposed;

    private GridServerProcess(Process process, IPEndPoint endPoint)
    {
        _process = process;
        _endpoint = endPoint;
    }

    /// <inheritdoc cref="IGridServerProcess.Process"/>
    public Process Process => _process;

    /// <inheritdoc cref="IGridServerProcess.IsOpen"/>
    public bool IsOpen => !_disposed && !_process.SafeGetHasExited();

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

        _process.Dispose();

        _disposed = true;
    }

    /// <summary>
    /// Start a new process with the specified executable path.
    /// </summary>
    /// <param name="executableName">Name of the executable.</param>
    /// <param name="workingDirectory">The working directory of the executable.</param>
    /// <param name="port">The port of the grid server.</param>
    /// <param name="args">The optional arguments</param>
    /// <returns>The process</returns>
    public static IGridServerProcess Start(
        string executableName,
        string? workingDirectory = null,
        int port = 53640,
        string? args = null
    )
    {
        if (TcpHealthCheck.GetProcessByHostnameAndPort(IPAddress.Loopback.ToString(), port, out var proc))
            return new GridServerProcess(proc, new IPEndPoint(IPAddress.Loopback, port));

        var startInfo = new ProcessStartInfo
        {
            FileName = executableName,
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

        var process = Process.Start(startInfo);

        return new GridServerProcess(process, new IPEndPoint(IPAddress.Loopback, port));
    }

    /// <summary>
    /// Discover all the grid servers.
    /// </summary>
    /// <param name="executableName">Name of the process.</param>
    /// <returns>The list of discovered processes.</returns>
    public static IEnumerable<IGridServerProcess> DiscoverProcesses(string executableName)
    {
        var tcpTable = ManagedIpHelper.GetExtendedTcpTable(true);
        var processes = Process.GetProcessesByName(executableName);
        var wrappedProcesses = new List<GridServerProcess>();

        foreach (var process in processes)
        {
            // Do not use the process if it is not owned by the current user.
            if (process.GetOwner() != Environment.UserName)
                continue;

            var tcpRow = tcpTable.FirstOrDefault(row => row.ProcessId == process.Id);

            if (tcpRow == default)
                continue;

            wrappedProcesses.Add(new GridServerProcess(process, tcpRow.LocalEndPoint));
        }

        return wrappedProcesses;
    }
}
