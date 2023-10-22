namespace Grid;

using System.Diagnostics;

using Logging;

/// <summary>
/// Represents the <see cref="IUnmanagedGridServerInstance"/> implementation for Grid Server Docker containers.
/// </summary>
public sealed class UnmanagedGridServerProcess : IUnmanagedGridServerInstance
{
    private readonly ILogger _Logger;

    /// <inheritdoc cref="IUnmanagedGridServerInstance.Id"/>
    public string Id => Process.RawProcess.Id.ToString();

    /// <summary>
    /// The container.
    /// </summary>
    public IGridServerProcess Process { get; }

    /// <summary>
    /// Contruct a new instance of <see cref="UnmanagedGridServerProcess"/>
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="process">The <see cref="IGridServerProcess"/></param>
    public UnmanagedGridServerProcess(ILogger logger, IGridServerProcess process)
    {
        _Logger = logger;

        Process = process;
    }

    /// <inheritdoc cref="IUnmanagedGridServerInstance.Kill"/>
    public void Kill()
    {
        Process.Kill();
    }
}
