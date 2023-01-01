namespace MFDLabs.Grid;

using System;
using System.Diagnostics;
using System.Net;
using Logging;

/// <summary>
/// Wrapper for a process owned by the arbiters.
/// </summary>
public interface IGridServerProcess : IDisposable
{
    /// <summary>
    /// The raw process.
    /// </summary>
    Process Process { get; }

    /// <summary>
    /// Is the process still running?
    /// </summary>
    bool IsOpen { get; }

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
}
