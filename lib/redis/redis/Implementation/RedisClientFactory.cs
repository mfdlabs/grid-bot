namespace Redis;

using System;

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

    /// <summary>
    /// Construct a new instance of <see cref="RedisClientFactory"/>
    /// </summary>
    /// <param name="useConnectionPooling">Is pooling enabled?</param>
    /// <param name="clientOptions">The <see cref="RedisClientOptions"/></param>
    /// <param name="pooledClientOptions">The <see cref="RedisPooledClientOptions"/></param>
    public RedisClientFactory(
        bool useConnectionPooling = false,
        RedisClientOptions clientOptions = null,
        RedisPooledClientOptions pooledClientOptions = null
    )
    {
        _ClientOptions = clientOptions;
        _RedisPooledClientOptions = pooledClientOptions;
        _UseConnectionPooling = useConnectionPooling;
    }

    /// <inheritdoc cref="IRedisClientFactory.GetRedisClient(RedisEndpoints, Action{Action{RedisEndpoints}}, Action{Exception})"/>
    public IRedisClient GetRedisClient(RedisEndpoints endpoints, Action<Action<RedisEndpoints>> monitorWireup, Action<Exception> errorHandler)
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
                    return _RedisClient = new RedisPooledClient(_CurrentEndpoints, null, _RedisPooledClientOptions);
                else
                    return _RedisClient = new RedisClient(_CurrentEndpoints, null, _ClientOptions);
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
