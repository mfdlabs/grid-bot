namespace Grid;

using System;
using System.Net;
using System.Diagnostics;

using Logging;

/// <summary>
/// Wrapper for a process owned by the arbiters.
/// </summary>
public interface IGridServerProcess : IDisposable
{
    /// <summary>
    /// The raw process.
    /// </summary>
    Process RawProcess { get; }

    /// <summary>
    /// Has the process exited?
    /// </summary>
    bool HasExited { get; }

    /// <summary>
    /// Get the endpoint of the instance.
    /// </summary>
    IPEndPoint EndPoint { get; }

    /// <summary>
    /// Is the process disposed?
    /// </summary>
    bool IsDisposed { get; }

    /// <inheritdoc cref="Process.Kill"/>
    void Kill();

    /// <summary>
    /// Set the logger to use.
    /// </summary>
    /// <param name="logger">The logger to use.</param>
    /// <exception cref="ArgumentNullException"><paramref name="logger"/> cannot be null.</exception>
    void SetLogger(ILogger logger);

    /// <summary>
    /// Start a new process with the specified executable path.
    /// </summary>
    /// <param name="executableName">Name of the executable.</param>
    /// <param name="workingDirectory">The working directory of the executable.</param>
    /// <param name="port">The port of the grid server.</param>
    /// <param name="args">The optional arguments</param>
    /// <returns>The process</returns>
    bool Start(string executableName, string workingDirectory = null, int port = 53640, string args = null);
}
