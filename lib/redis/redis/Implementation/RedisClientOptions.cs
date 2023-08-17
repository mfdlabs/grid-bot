namespace Redis;

using System;

using StackExchange.Redis;

/// <summary>
/// Default implementation for Redis Client Options.
/// </summary>
public sealed class RedisClientOptions : RedisClientOptionsBase
{
    /// <summary>
    /// Construct a new instance of <see cref="RedisClientOptions"/>
    /// </summary>
    /// <param name="connectionBuilder">The <see cref="IConnectionBuilder"/></param>
    /// <param name="reconnectRetryPolicy">The <see cref="IReconnectRetryPolicy"/></param>
    /// <param name="syncTimeout">The sync timeout</param>
    /// <param name="disableSubscriptions">Are subs disabled?</param>
    /// <param name="connectTimeoutGetter">The getter for connection timeout</param>
    /// <param name="responseTimeoutGetter">The getter for response timeout</param>
    public RedisClientOptions(
        IConnectionBuilder connectionBuilder = null,
        IReconnectRetryPolicy reconnectRetryPolicy = null,
        TimeSpan? syncTimeout = null,
        bool disableSubscriptions = false,
        Func<TimeSpan> connectTimeoutGetter = null,
        Func<TimeSpan> responseTimeoutGetter = null
    ) : base(
            connectionBuilder, 
            reconnectRetryPolicy, 
            syncTimeout, 
            disableSubscriptions, 
            connectTimeoutGetter, 
            responseTimeoutGetter
    )
    {
    }
}
