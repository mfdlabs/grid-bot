namespace Redis;

/// <summary>
/// Provider for redis clients.
/// </summary>
public interface IRedisClientProvider
{
    /// <summary>
    /// The Redis client.
    /// </summary>
    IRedisClient Client { get; }
}
