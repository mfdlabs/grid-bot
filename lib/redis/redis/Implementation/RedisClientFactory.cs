namespace Redis;

using System;

using Instrumentation;

/// <summary>
/// Redis Client Factory
/// </summary>
public class RedisClientFactory : IRedisClientFactory
{
    private readonly object _Lock = new();

    private bool _MonitorStarted;
    private volatile IRedisClient _RedisClient;
    private RedisEndpoints _CurrentEndpoints;

    private readonly bool _UseConnectionPooling;
    private readonly RedisPooledClientOptions _RedisPooledClientOptions;
    private readonly RedisClientOptions _ClientOptions;
    private readonly ICounterRegistry _CounterRegistry;

    /// <summary>
    /// Construct a new instance of <see cref="RedisClientFactory"/>
    /// </summary>
    /// <param name="counterRegistry">The <see cref="ICounterRegistry"/></param>
    /// <param name="useConnectionPooling">Is pooling enabled?</param>
    /// <param name="clientOptions">The <see cref="RedisClientOptions"/></param>
    /// <param name="pooledClientOptions">The <see cref="RedisPooledClientOptions"/></param>
    public RedisClientFactory(
        ICounterRegistry counterRegistry,
        bool useConnectionPooling = false,
        RedisClientOptions clientOptions = null,
        RedisPooledClientOptions pooledClientOptions = null
    )
    {
        _ClientOptions = clientOptions;
        _RedisPooledClientOptions = pooledClientOptions;
        _UseConnectionPooling = useConnectionPooling;
        _CounterRegistry = counterRegistry;
    }

    /// <inheritdoc cref="IRedisClientFactory.GetRedisClient(RedisEndpoints, Action{Action{RedisEndpoints}}, string, Action{Exception})"/>
    public IRedisClient GetRedisClient(RedisEndpoints endpoints, Action<Action<RedisEndpoints>> monitorWireup, string performanceMonitorCategory, Action<Exception> errorHandler)
    {
        if (_RedisClient != null) return _RedisClient;

        lock (_Lock)
        {
            if (_RedisClient != null) return _RedisClient;

            if (!_MonitorStarted)
            {
                monitorWireup(newEndpoints => Refresh(newEndpoints, errorHandler));
                _MonitorStarted = true;
            }

            try
            {
                _CurrentEndpoints = endpoints;

                if (_UseConnectionPooling)
                    return _RedisClient = new RedisPooledClient(_CounterRegistry, _CurrentEndpoints, performanceMonitorCategory, null, _RedisPooledClientOptions);
                else
                    return _RedisClient = new RedisClient(_CounterRegistry, _CurrentEndpoints, performanceMonitorCategory, null, _ClientOptions);
            }
            catch (Exception ex)
            {
                errorHandler(ex);

                return null;
            }
        }
    }

    private void Refresh(RedisEndpoints redisEndpoints, Action<Exception> errorHandler)
    {
        try
        {
            lock (_Lock)
            {
                if (_RedisClient != null && !redisEndpoints.HasTheSameEndpoints(_CurrentEndpoints))
                {
                    _CurrentEndpoints = redisEndpoints;

                    _RedisClient.Refresh(_CurrentEndpoints);
                }
            }
        }
        catch (Exception ex)
        {
            errorHandler(ex);
        }
    }
}
