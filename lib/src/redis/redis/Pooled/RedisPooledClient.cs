namespace Redis;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using StackExchange.Redis;

using Hashing;

/// <summary>
/// Default implementation for a pooled Redis client.
/// </summary>
public class RedisPooledClient : RedisClientBase<RedisPooledClientOptions>
{
    private RedisConnectionMultiplexerPool[] _Pools;
    private ConsistentHash<ConsistentHashConnectionWrapperBase> _NodeProvider;

    private readonly ParallelOptions _ParallelOptions = new()
    {
        MaxDegreeOfParallelism = 10
    };
    private readonly Func<DateTime> _GetCurrentTimeFunc;

    /// <summary>
    /// Construct a new instance of <see cref="RedisPooledClient"/>
    /// </summary>
    /// <param name="redisEndpoints">The Redis EndPoints</param>
    /// <param name="exceptionHandler">The exception handler.</param>
    /// <param name="clientOptions">The <see cref="RedisPooledClientOptions"/></param>
    /// <param name="getCurrentTimeFunc">The Utc Now getter.</param>
    public RedisPooledClient(
        IEnumerable<string> redisEndpoints, 
        Action<Exception> exceptionHandler = null,
        RedisPooledClientOptions clientOptions = null, 
        Func<DateTime> getCurrentTimeFunc = null
    ) 
        : base(
            clientOptions ?? new RedisPooledClientOptions(), 
            exceptionHandler
        )
    {
        _GetCurrentTimeFunc = getCurrentTimeFunc ?? (() => DateTime.UtcNow);
        
        ChangePools(redisEndpoints.ToArray());
    }

    /// <summary>
    /// Construct a new instance of <see cref="RedisPooledClient"/>
    /// </summary>
    /// <param name="redisEndpoints">The Redis EndPoints</param>
    /// <param name="exceptionHandler">The exception handler.</param>
    /// <param name="clientOptions">The <see cref="RedisPooledClientOptions"/></param>
    /// <param name="getCurrentTimeFunc">The Utc Now getter.</param>
    public RedisPooledClient(
        RedisEndpoints redisEndpoints,
        Action<Exception> exceptionHandler = null, 
        RedisPooledClientOptions clientOptions = null, 
        Func<DateTime> getCurrentTimeFunc = null
    ) 
        : this(
            redisEndpoints?.Endpoints, 
            exceptionHandler,
            clientOptions, 
            getCurrentTimeFunc
        )
    {
    }

    /// <inheritdoc cref="RedisClientBase{TOptions}.GetDatabase(string)"/>
    public override IDatabase GetDatabase(string partitionKey)
    {
        if (_Pools.Length == 1)
            return _Pools[0].GetConnectionMultiplexer().GetDatabase();

        return _NodeProvider.GetNode(partitionKey).Database;
    }

    /// <inheritdoc cref="RedisClientBase{TOptions}.GetServer(string)"/>
    public override IServer GetServer(string partitionKey)
    {
        if (_Pools.Length == 1)
            return GetServerFromMultiplexer(_Pools[0].GetConnectionMultiplexer());

        return _NodeProvider.GetNode(partitionKey).Server;
    }

    /// <inheritdoc cref="RedisClientBase{TOptions}.GetSubscriber(string)"/>
    public override ISubscriber GetSubscriber(string partitionKey)
    {
        if (_Pools.Length == 1)
            return _Pools[0].Subscriber;

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
        => (from p in _Pools
            select p.GetConnectionMultiplexer().GetDatabase()).ToList();

    /// <inheritdoc cref="RedisClientBase{TOptions}.GetAllSubscribers"/>
    public override IReadOnlyCollection<ISubscriber> GetAllSubscribers() 
        => (from p in _Pools
            select p.Subscriber).ToList();

    /// <inheritdoc cref="RedisClientBase{TOptions}.GetAllServers"/>
    public override IReadOnlyCollection<IServer> GetAllServers() 
        => (from p in _Pools
            select GetServerFromMultiplexer(p.GetConnectionMultiplexer())).ToList();

    /// <inheritdoc cref="RedisClientBase{TOptions}.Refresh(string[])"/>
    public override void Refresh(string[] redisEndpoints)
    {
        ChangePools(redisEndpoints);

        OnRefreshed();
    }

    /// <inheritdoc cref="RedisClientBase{TOptions}.Close"/>
    public override void Close() => Parallel.ForEach(_Pools, _ParallelOptions, p => p.Close());

    private void ChangePools(string[] redisEndpoints)
    {
        var pools = _Pools ?? Array.Empty<RedisConnectionMultiplexerPool>();
        var keepingOldPools = (from op in pools
                               where redisEndpoints.Contains(GetMultiplexerDescriptor(op.PrimaryConnection))
                               select op).ToArray();

        var actualPools = pools.Except(keepingOldPools);

        var tasks = (from re in redisEndpoints
                      where keepingOldPools.All(kop => kop.PrimaryConnection.GetEndPoints()[0].ToString() != re)
                      select re into endpoint
                      select CreatePoolAsync(endpoint, RedisClientOptions)).ToArray();

        Task.WaitAll(tasks);

        _Pools = keepingOldPools.Concat(from t in tasks
                                        select t.Result).ToArray();

        var nodes = (from p in _Pools
                     select new RedisConnectionMultiplexerPoolWrapper(p, p.BaseConfiguration)).ToList();
        var nodeProvider = new ConsistentHash<ConsistentHashConnectionWrapperBase>();

        nodeProvider.Init(nodes);

        Interlocked.Exchange(ref _NodeProvider, nodeProvider);

        foreach (var pool in actualPools)
            pool.Dispose();

        static string GetMultiplexerDescriptor(IConnectionMultiplexer cm) => cm.GetEndPoints()[0].ToString();
    }

    private async Task<RedisConnectionMultiplexerPool> CreatePoolAsync(string redisEndpoint, RedisPooledClientOptions clientOptions)
    {
        var opts = base.GetConfigurationOptions(redisEndpoint);
        var pool = new RedisConnectionMultiplexerPool(opts, clientOptions);

        await pool.ConnectAsync().ConfigureAwait(false);

        return pool;
    }
}
