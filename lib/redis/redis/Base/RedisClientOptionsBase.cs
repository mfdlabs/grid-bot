namespace Redis;

using System;

using StackExchange.Redis;

/// <summary>
/// Base class for Redis Client Options.
/// </summary>
public abstract class RedisClientOptionsBase
{
    /// <summary>
    /// The reconnect retry policy
    /// </summary>
    public IReconnectRetryPolicy ReconnectRetryPolicy { get; }

    /// <summary>
    /// The timeout for sync.
    /// </summary>
    public TimeSpan? SyncTimeout { get; }

    /// <summary>
    /// The connection builder.
    /// </summary>
    public IConnectionBuilder ConnectionBuilder { get; }

    /// <summary>
    /// Should sub be disabled?
    /// </summary>
    public bool DisableSubscriptions { get; }

    /// <summary>
    /// The timeout for connection.
    /// </summary>
    public Func<TimeSpan> ConnectTimeout { get; }

    /// <summary>
    /// The timeout for recieving a response.
    /// </summary>
    public Func<TimeSpan> ResponseTimeout { get; }

    /// <summary>
    /// Construct a new instance of <see cref="RedisClientOptionsBase"/>
    /// </summary>
    /// <param name="connectionBuilder">The <see cref="IConnectionBuilder"/></param>
    /// <param name="reconnectRetryPolicy">The <see cref="IReconnectRetryPolicy"/></param>
    /// <param name="syncTimeout">The sync timeout</param>
    /// <param name="disableSubscriptions">Are subs disabled?</param>
    /// <param name="connectTimeoutGetter">The getter for connection timeout</param>
    /// <param name="responseTimeoutGetter">The getter for response timeout</param>
    protected RedisClientOptionsBase(
        IConnectionBuilder connectionBuilder = null, 
        IReconnectRetryPolicy reconnectRetryPolicy = null, 
        TimeSpan? syncTimeout = null, 
        bool disableSubscriptions = false, 
        Func<TimeSpan> connectTimeoutGetter = null, 
        Func<TimeSpan> responseTimeoutGetter = null
    )
    {
        ConnectionBuilder = connectionBuilder;
        ReconnectRetryPolicy = reconnectRetryPolicy;
        SyncTimeout = syncTimeout;
        DisableSubscriptions = disableSubscriptions;

        ConnectTimeout = connectTimeoutGetter ?? (() => TimeSpan.FromSeconds(1));
        ResponseTimeout = responseTimeoutGetter ?? (() => TimeSpan.FromSeconds(1));
    }
}
