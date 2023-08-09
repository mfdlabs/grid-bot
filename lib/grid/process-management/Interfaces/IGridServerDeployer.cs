namespace Grid;

using System;
using System.Collections.Generic;

/// <summary>
/// A managed wrapper for <see cref="IGridServerProcess"/>.
/// </summary>
public interface IGridServerDeployer
{
    /// <summary>
    /// The managed processes that are associated with this deployer.
    /// </summary>
    IEnumerable<IGridServerProcess> Processes { get; }

    /// <summary>
    /// The name of the grid server's executable.
    /// </summary>
    string ExecutableName { get; }

    /// <summary>
    /// Path to the grid server's executable.
    /// </summary>
    string ExecutablePath { get; }

    /// <summary>
    /// Kill all the processes associated with this deployer.
    /// </summary>
    void KillAll();

    /// <summary>
    /// Create a new process.
    /// </summary>
    /// <param name="port">The port of the new instance. If null, a random port will be used.</param>
    /// <param name="proccess">The newly created process.</param>
    /// <param name="exception">An exception if any occurred.</param>
    /// <returns>True if the process was launched successfuly.</returns>
    bool CreateProcess(int? port, out IGridServerProcess proccess, out Exception exception);

    /// <summary>
    /// Get a process by its port.
    /// </summary>
    /// <param name="port">The port of the process.</param>
    /// <returns>The process.</returns>
    IGridServerProcess GetProcess(int port);

    /// <summary>
    /// Get a process by PID.
    /// </summary>
    /// <param name="pid">The ID of the process.</param>
    /// <returns>The process.</returns>
    IGridServerProcess GetProcessByPid(int pid);

    /// <summary>
    /// Kill a process by its port.
    /// </summary>
    /// <param name="port">The port of the process.</param>
    /// <param name="exception">An exception if any occurred.</param>
    /// <returns>True if the process was killed successfuly.</returns>
    bool KillProcess(int port, out Exception exception);

    /// <summary>
    /// Kill a process.
    /// </summary>
    /// <param name="process">The process.</param>
    /// <param name="exception">An exception if any occurred.</param>
    /// <returns>True if the process was killed successfuly.</returns>
    bool KillProcess(IGridServerProcess process, out Exception exception);

    /// <summary>
    /// Kill a process by PID.
    /// </summary>
    /// <param name="pid">The ID of the process.</param>
    /// <param name="exception">An exception if any occurred.</param>
    /// <returns>True if the process was killed successfuly.</returns>
    bool KillProcessByPid(int pid, out Exception exception);

    /// <summary>
    /// Get a random process.
    /// </summary>
    /// <returns>The process.</returns>
    IGridServerProcess GetRandomProcess();

    /// <summary>
    /// Discover instances on the current machine and add them to the list of processes.
    /// </summary>
    /// <returns>The list of discovered instances.</returns>
    IEnumerable<IGridServerProcess> DiscoverInstances();
}
