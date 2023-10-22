namespace Redis;

using System;

using StackExchange.Redis;

/// <summary>
/// Client options for the Pooled Redis Client.
/// </summary>
public sealed class RedisPooledClientOptions : RedisClientOptionsBase
{
    /// <summary>
    /// Gets the size of the pool.
    /// </summary>
    public int PoolSize { get; }

    /// <summary>
    /// Gets the max reconnect timeout.
    /// </summary>
    public int MaxReconnectTimeout { get; }

    /// <summary>
    /// Construct a new instance of <see cref="RedisPooledClientOptions"/>
    /// </summary>
    /// <param name="poolSize">The pool size</param>
    /// <param name="maxReconnectTimeout">The max reconnect timeout</param>
    /// <param name="connectionBuilder">The <see cref="IConnectionBuilder"/></param>
    /// <param name="reconnectRetryPolicy">The <see cref="IReconnectRetryPolicy"/></param>
    /// <param name="syncTimeout">The sync timeout</param>
    /// <param name="disableSubcriptions">Are subs disabled?</param>
    /// <param name="connectTimeoutGetter">The getter for connection timeout</param>
    /// <param name="responseTimeoutGetter">The getter for response timeout</param>
    public RedisPooledClientOptions(
        int poolSize = 1, 
        int maxReconnectTimeout = 0, 
        IConnectionBuilder connectionBuilder = null, 
        IReconnectRetryPolicy reconnectRetryPolicy = null, 
        TimeSpan? syncTimeout = null, 
        bool disableSubcriptions = false,
        Func<TimeSpan> connectTimeoutGetter = null, 
        Func<TimeSpan> responseTimeoutGetter = null
    ) 
        : base(
            connectionBuilder, 
            reconnectRetryPolicy, 
            syncTimeout, 
            disableSubcriptions, 
            connectTimeoutGetter, 
            responseTimeoutGetter
        )
    {
        if (poolSize < 1) throw new ArgumentOutOfRangeException(nameof(poolSize));

        PoolSize = poolSize;
        MaxReconnectTimeout = maxReconnectTimeout;
    }
}
