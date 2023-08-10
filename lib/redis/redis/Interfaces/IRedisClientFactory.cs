namespace Redis; 

using System;

/// <summary>
/// Factory for spitting out Redis clients.
/// </summary>
public interface IRedisClientFactory
{
    /// <summary>
    /// Get a new Redis client
    /// </summary>
    /// <param name="endpoints">The redis endpoints</param>
    /// <param name="monitorWireup">The monitoring wireup</param>
    /// <param name="performanceMonitorCategory">The perfmon monitor category</param>
    /// <param name="errorHandler">The exception handler.</param>
    /// <returns>The Redis client.</returns>
    IRedisClient GetRedisClient(RedisEndpoints endpoints, Action<Action<RedisEndpoints>> monitorWireup, string performanceMonitorCategory, Action<Exception> errorHandler);
}
