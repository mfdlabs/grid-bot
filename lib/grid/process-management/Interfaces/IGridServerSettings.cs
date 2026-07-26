namespace Grid.ProcessManagement;

using Core;

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

    /// <summary>
    /// The maximum amount of GridServer Service memory in bytes.
    /// </summary>
    long GridServerMaxMemoryInBytes { get; }

    /// <summary>
    /// Determines if verbose logging is enabled or not.
    /// </summary>
    bool VerboseLoggingEnabled { get; }

    /// <summary>
    /// The name of the file to cache app settings in.
    /// </summary>
    string GridServerApplicationSettingsFileName { get; }
}
