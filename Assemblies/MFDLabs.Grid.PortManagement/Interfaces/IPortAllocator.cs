namespace MFDLabs.Grid
{
    /// <summary>
    /// A class to allocate ports.
    /// </summary>
    public interface IPortAllocator
    {
        /// <summary>
        /// Find the next available port in a range.
        /// </summary>
        /// <returns>The port.</returns>
        /// <exception cref="System.TimeoutException">Failed to find an open port.</exception>
        int FindNextAvailablePort();

        /// <summary>
        /// Remove a port from the memory cache.
        /// </summary>
        /// <param name="port">The port.</param>
        void RemovePortFromCacheIfExists(int port);
    }
}
