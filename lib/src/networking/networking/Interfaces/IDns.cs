namespace Networking;

using System.Net;

/// <summary>
/// Interface for resolving hostnames.
/// </summary>
public interface IDns
{
    /// <summary>
    /// Get the current hostname.
    /// </summary>
    /// <returns>The hostname.</returns>
    string GetHostName();

    /// <summary>
    /// Get the IP hostname for the specified hostname.
    /// </summary>
    /// <param name="hostNameOrAddress">Host or address to query.</param>
    /// <returns>The host entry.</returns>
    IPHostEntry GetHostEntry(string hostNameOrAddress);
}
