using System;

namespace Grid;

/// <summary>
/// Represents the settings used across the Arbiter stack.
/// </summary>
public interface ISettings
{
    /// <summary>
    /// Gets the default lease for a <see cref="ILeasedGridServerInstance"/>
    /// </summary>
    TimeSpan DefaultLeasedGridServerInstanceLease { get; }

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
