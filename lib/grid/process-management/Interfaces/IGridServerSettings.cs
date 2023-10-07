namespace Grid;

/// <summary>
/// Represents the Grid Server settings.
/// </summary>
public interface IGridServerProcessSettings : IJobManagerSettings
{
    /// <summary>
    /// Gets the name of the executable used by process-based grid-servers.
    /// </summary>
    string GridServerExecutableName { get; }

    /// <summary>
    /// Gets the name of the Windows Registry Key used by process-based grid-servers.
    /// </summary>
    string GridServerRegistryKeyName { get; }

    /// <summary>
    /// Gets the name of the Windows Registry Value used by process-based grid-servers.
    /// </summary>
    string GridServerRegistryValueName { get; }
}
