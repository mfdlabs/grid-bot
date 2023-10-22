namespace Redis;

using System.Threading.Tasks;

using StackExchange.Redis;

/// <summary>
/// Interface for build connection multiplexers
/// </summary>
public interface IConnectionBuilder
{
    /// <summary>
    /// Create a new connection
    /// </summary>
    /// <param name="configuration">The configuration</param>
    /// <returns>The new connection</returns>
    Task<IConnectionMultiplexer> CreateConnectionMultiplexerAsync(ConfigurationOptions configuration);
}
