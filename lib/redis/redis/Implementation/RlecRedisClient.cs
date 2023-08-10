namespace Redis;

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using StackExchange.Redis;

using Instrumentation;

/// <summary>
/// Redis Labs Enterprise Client.
/// </summary>
public class RlecRedisClient : RedisClientBase<RedisClientOptions>
{
    private readonly object _MultiplexerCreationSync = new();
    private readonly Random _Random = new();

    private string[] _RedisEndpoints;
    private IConnectionMultiplexer _Multiplexer;

    /// <summary>
    /// Construct a new instance of <see cref="RlecRedisClient"/>
    /// </summary>
    /// <param name="counterRegistry">The <see cref="ICounterRegistry"/></param>
    /// <param name="redisEndpoints">The Redis EndPoints</param>
    /// <param name="performanceMonitorCategory">The performance monitor category</param>
    /// <param name="exceptionHandler">The exception handler.</param>
    /// <param name="clientOptions">The <see cref="RedisClientOptions"/></param>
    /// <exception cref="ArgumentNullException"><paramref name="redisEndpoints"/> cannot be null.</exception>
    public RlecRedisClient(
        ICounterRegistry counterRegistry, 
        IEnumerable<string> redisEndpoints, 
        string performanceMonitorCategory, 
        Action<Exception> exceptionHandler = null, 
        RedisClientOptions clientOptions = null
    )
        : base(
            counterRegistry,
            performanceMonitorCategory,
            clientOptions ?? new RedisClientOptions(), 
            exceptionHandler
        )
    {
        _RedisEndpoints = redisEndpoints.ToArray();

        PickRandomEndpoint(null);
    }

    /// <summary>
    /// Construct a new instance of <see cref="RlecRedisClient"/>
    /// </summary>
    /// <param name="counterRegistry">The <see cref="ICounterRegistry"/></param>
    /// <param name="redisEndpoints">The Redis EndPoints</param>
    /// <param name="performanceMonitorCategory">The performance monitor category</param>
    /// <param name="exceptionHandler">The exception handler.</param>
    /// <param name="redisClientOptions">The <see cref="RedisClientOptions"/></param>
    public RlecRedisClient(
        ICounterRegistry counterRegistry,
        RedisEndpoints redisEndpoints, 
        string performanceMonitorCategory, 
        Action<Exception> exceptionHandler = null,
        RedisClientOptions redisClientOptions = null
    )
        : this(
            counterRegistry, 
            redisEndpoints?.Endpoints,
            performanceMonitorCategory,
            exceptionHandler, 
            redisClientOptions
        )
    {
    }

    /// <inheritdoc cref="RedisClientBase{TOptions}.Refresh(string[])"/>
    public override void Refresh(string[] redisEndpoints)
    {
        _RedisEndpoints = redisEndpoints;

        PickRandomEndpoint(null);

        OnRefreshed();
    }

    /// <inheritdoc cref="RedisClientBase{TOptions}.GetServer(string)"/>
    public override IServer GetServer(string partitionKey)
        => GetServerFromMultiplexer(_Multiplexer);

    /// <inheritdoc cref="RedisClientBase{TOptions}.GetAllServers"/>
    public override IReadOnlyCollection<IServer> GetAllServers()
        => new[] { GetServerFromMultiplexer(_Multiplexer) };

    /// <inheritdoc cref="RedisClientBase{TOptions}.GetDatabase(string)"/>
    public override IDatabase GetDatabase(string partitionKey)
        => _Multiplexer.GetDatabase();

    /// <inheritdoc cref="RedisClientBase{TOptions}.GetSubscriber(string)"/>
    public override ISubscriber GetSubscriber(string partitionKey)
        => _Multiplexer.GetSubscriber();

    /// <inheritdoc cref="RedisClientBase{TOptions}.GetDatabases(IReadOnlyCollection{string})"/>
    public override IDictionary<IDatabase, IReadOnlyCollection<string>> GetDatabases(IReadOnlyCollection<string> partitionKeys)
    {
        if (partitionKeys != null)
            if (!partitionKeys.Any(pk => pk == null))
                return new Dictionary<IDatabase, IReadOnlyCollection<string>>
                {
                    { _Multiplexer.GetDatabase(), partitionKeys }
                };

        throw new ArgumentNullException(nameof(partitionKeys));
    }

    /// <inheritdoc cref="RedisClientBase{TOptions}.GetAllDatabases"/>
    public override IReadOnlyCollection<IDatabase> GetAllDatabases()
        => new[] { _Multiplexer.GetDatabase() };

    /// <inheritdoc cref="RedisClientBase{TOptions}.GetAllSubscribers"/>
    public override IReadOnlyCollection<ISubscriber> GetAllSubscribers()
        => new[] { _Multiplexer.GetSubscriber() };

    /// <inheritdoc cref="RedisClientBase{TOptions}.Close"/>
    public override void Close() => _Multiplexer.Close(false);

    private void MultiplexerOnConnectionFailed(object sender, ConnectionFailedEventArgs args)
        => PickRandomEndpoint(ConfigurationOptions.Parse(((ConnectionMultiplexer)sender).Configuration).EndPoints[0].ToString());

    private void PickRandomEndpoint(string excludedEndpoint)
    {
        var multiplexer = _Multiplexer;

        _Multiplexer = CreateMultiplexer();

        DisposeMultiplexer(multiplexer, true);

        IConnectionMultiplexer CreateMultiplexer()
        {
            lock (_MultiplexerCreationSync)
            {
                var endpoints = new HashSet<string>(_RedisEndpoints);
                if (excludedEndpoint != null)
                    endpoints.Remove(excludedEndpoint);

                while (endpoints.Count > 0)
                {
                    var e = endpoints.ToArray<string>();
                    int rand = _Random.Next(0, e.Length);
                    var endpoint = e[rand];

                    var connectionMultiplexer = ConnectMultiplexer(endpoint);
                    if (connectionMultiplexer.IsConnected)
                    {
                        connectionMultiplexer.ConnectionFailed += MultiplexerOnConnectionFailed;
                        return connectionMultiplexer;
                    }

                    DisposeMultiplexer(connectionMultiplexer, false);

                    endpoints.Remove(endpoint);
                }

                int r2 = _Random.Next(0, _RedisEndpoints.Length);
                var redisEndpoint = _RedisEndpoints[r2];

                var mul = ConnectMultiplexer(redisEndpoint);
                mul.ConnectionFailed += MultiplexerOnConnectionFailed;

                return mul;
            }
        }
    }

    private void DisposeMultiplexer(IConnectionMultiplexer multiplexer, bool delayedDisposal)
    {
        if (multiplexer == null) return;
        multiplexer.ConnectionFailed -= MultiplexerOnConnectionFailed;

        if (!delayedDisposal)
        {
            multiplexer.Dispose();
            return;
        }

        Task.Delay(5000).ContinueWith(t =>
        {
            try
            {
                multiplexer.Dispose();
            }
            catch
            {
            }
        });
    }
}
