namespace Redis;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using StackExchange.Redis;

using Hashing;
using Instrumentation;

/// <summary>
/// Default Redis Client.
/// </summary>
public class RedisClient : RedisClientBase<RedisClientOptions>
{
    private IConnectionMultiplexer[] _Multiplexers;
    private ConsistentHash<ConsistentHashConnectionWrapperBase> _NodeProvider;

    /// <summary>
    /// Construct a new instance of <see cref="RedisClient"/>
    /// </summary>
    /// <param name="counterRegistry">The <see cref="ICounterRegistry"/></param>
    /// <param name="redisEndpoints">The Redis EndPoints</param>
    /// <param name="performanceMonitorCategory">The performance monitor category</param>
    /// <param name="exceptionHandler">The exception handler.</param>
    /// <param name="redisClientOptions">The <see cref="RedisClientOptions"/></param>
    /// <exception cref="ArgumentNullException"><paramref name="redisEndpoints"/> cannot be null.</exception>
    public RedisClient(
        ICounterRegistry counterRegistry, 
        IEnumerable<string> redisEndpoints, 
        string performanceMonitorCategory, 
        Action<Exception> exceptionHandler = null, 
        RedisClientOptions redisClientOptions = null
    ) 
        : base(
            counterRegistry, 
            performanceMonitorCategory, 
            redisClientOptions ?? new RedisClientOptions(), 
            exceptionHandler
        )
    {
        if (redisEndpoints == null) throw new ArgumentNullException(nameof(redisEndpoints));

        ChangeMultiplexers(redisEndpoints.ToArray());
    }

    /// <summary>
    /// Construct a new instance of <see cref="RedisClient"/>
    /// </summary>
    /// <param name="counterRegistry">The <see cref="ICounterRegistry"/></param>
    /// <param name="redisEndpoints">The Redis EndPoints</param>
    /// <param name="performanceMonitorCategory">The performance monitor category</param>
    /// <param name="exceptionHandler">The exception handler.</param>
    /// <param name="redisClientOptions">The <see cref="RedisClientOptions"/></param>
    public RedisClient(
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

    /// <inheritdoc cref="RedisClientBase{TOptions}.GetDatabase(string)"/>
    public override IDatabase GetDatabase(string partitionKey)
    {
        if (_Multiplexers.Length == 1) return _Multiplexers[0].GetDatabase();

        return _NodeProvider.GetNode(partitionKey).Database;
    }

    /// <inheritdoc cref="RedisClientBase{TOptions}.GetServer(string)"/>
    public override IServer GetServer(string partitionKey)
    {
        if (_Multiplexers.Length == 1) return GetServerFromMultiplexer(_Multiplexers[0]);

        return _NodeProvider.GetNode(partitionKey).Server;
    }

    /// <inheritdoc cref="RedisClientBase{TOptions}.GetSubscriber(string)"/>
    public override ISubscriber GetSubscriber(string partitionKey)
    {
        if (_Multiplexers.Length == 1) return _Multiplexers[0].GetSubscriber();

        return _NodeProvider.GetNode(partitionKey).Subscriber;
    }

    /// <inheritdoc cref="RedisClientBase{TOptions}.GetDatabases(IReadOnlyCollection{string})"/>
    public override IDictionary<IDatabase, IReadOnlyCollection<string>> GetDatabases(IReadOnlyCollection<string> partitionKeys)
    {
        if (partitionKeys != null)
            if (!partitionKeys.Any(pk => pk == null))
                return ConsistentHashConnectionWrapperBase.GetDatabasesByConsistentHashingAlgorithm(partitionKeys, _NodeProvider);

        throw new ArgumentNullException(nameof(partitionKeys));
    }

    /// <inheritdoc cref="RedisClientBase{TOptions}.GetAllDatabases"/>
    public override IReadOnlyCollection<IDatabase> GetAllDatabases() 
        => (from d in _Multiplexers
            select d.GetDatabase()).ToList();

    /// <inheritdoc cref="RedisClientBase{TOptions}.GetAllSubscribers"/>
    public override IReadOnlyCollection<ISubscriber> GetAllSubscribers() 
        => (from m in _Multiplexers
            select m.GetSubscriber()).ToList();

    /// <inheritdoc cref="RedisClientBase{TOptions}.GetAllServers"/>
    public override IReadOnlyCollection<IServer> GetAllServers() 
        => _Multiplexers.Select(GetServerFromMultiplexer).ToList();

    /// <inheritdoc cref="RedisClientBase{TOptions}.Refresh(string[])"/>
    public override void Refresh(string[] redisEndpoints)
    {
        ChangeMultiplexers(redisEndpoints);
        OnRefreshed();
    }

    /// <inheritdoc cref="RedisClientBase{TOptions}.Close"/>
    public override void Close() 
        => Parallel.ForEach(_Multiplexers, m => m.Close(false));

    private void ChangeMultiplexers(string[] redisEndpoints)
    {
        var oldPools = _Multiplexers ?? Array.Empty<IConnectionMultiplexer>();
        var multiplexers = oldPools.Where(o => redisEndpoints.Any(re => re == GetMultiplexerDescriptor(o))).ToArray();

        var actualMultiplexers = oldPools.Except(multiplexers);

        var tasks = redisEndpoints.Where(re => oldPools.All(o => GetMultiplexerDescriptor(o) != re)).Select(ConnectMultiplexerAsync).ToArray();

        Task.WaitAll(tasks);

        _Multiplexers = multiplexers.Concat(tasks.Select(m => m.Result)).ToArray();

        var nodes = _Multiplexers.Select(cm => new RedisConnectionWrapper(cm, GetConfigurationFromMultiplexer(cm))).ToList();
        var nodeProvider = new ConsistentHash<ConsistentHashConnectionWrapperBase>();

        nodeProvider.Init(nodes);

        Interlocked.Exchange(ref _NodeProvider, nodeProvider);

        foreach (var multiplexer in actualMultiplexers)
            multiplexer.Dispose();

        static string GetMultiplexerDescriptor(IConnectionMultiplexer cm) => cm.GetEndPoints()[0].ToString();
    }

    private static ConfigurationOptions GetConfigurationFromMultiplexer(IConnectionMultiplexer multiplexer) => ConfigurationOptions.Parse(multiplexer.Configuration);
}
