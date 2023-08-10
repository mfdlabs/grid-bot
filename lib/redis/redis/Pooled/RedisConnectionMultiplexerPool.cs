namespace Redis;

using System;
using System.Threading;
using System.Threading.Tasks;

using StackExchange.Redis;

internal class RedisConnectionMultiplexerPool
{
    private RedisConnectionMultiplexerWatcher[] _ConnectionWatchers;
    private int _RoundRobinIndex;

    private readonly IConnectionBuilder _ConnectionBuilder;
    private readonly ParallelOptions _ParallelOptions = new()
    {
        MaxDegreeOfParallelism = 10
    };

    public ConfigurationOptions BaseConfiguration { get; }
    public RedisPooledClientOptions ClientOptions { get; }
    public IConnectionMultiplexer PrimaryConnection
    {
        get
        {
            CheckIfReady();
            return _ConnectionWatchers[0].Connection;
        }
    }
    public int Size
    {
        get
        {
            CheckIfReady();
            return _ConnectionWatchers.Length;
        }
    }
    public ISubscriber Subscriber => PrimaryConnection.GetSubscriber();

    public RedisConnectionMultiplexerPool(ConfigurationOptions baseConfiguration, RedisPooledClientOptions clientOptions)
    {
        BaseConfiguration = baseConfiguration ?? throw new ArgumentNullException(nameof(baseConfiguration));
        ClientOptions = clientOptions ?? throw new ArgumentNullException(nameof(clientOptions));

        _ConnectionBuilder = clientOptions.ConnectionBuilder ?? new DefaultConnectionBuilder();
        if (clientOptions.ReconnectRetryPolicy != null)
            BaseConfiguration.ReconnectRetryPolicy = clientOptions.ReconnectRetryPolicy;

        _RoundRobinIndex = -1;
    }

    public IConnectionMultiplexer GetConnectionMultiplexer()
    {
        CheckIfReady();
        int idx = Interlocked.Increment(ref _RoundRobinIndex);
        if (idx < 0)
            idx -= int.MinValue;

        idx %= _ConnectionWatchers.Length;

        return _ConnectionWatchers[idx].Connection;
    }

    public void Close()
    {
        CheckIfReady();

        Parallel.ForEach(_ConnectionWatchers, _ParallelOptions, w => w.Close());
    }

    public void Dispose()
    {
        CheckIfReady();

        Parallel.ForEach(_ConnectionWatchers, _ParallelOptions, w => w.Dispose());
    }

    public async Task ConnectAsync()
    {
        var connectTasks = new Task<IConnectionMultiplexer>[ClientOptions.PoolSize];
        for (int i = 0; i < ClientOptions.PoolSize; i++)
            connectTasks[i] = _ConnectionBuilder.CreateConnectionMultiplexerAsync(BaseConfiguration);

        var connections = await Task.WhenAll(connectTasks).ConfigureAwait(false);
        var watchers = new RedisConnectionMultiplexerWatcher[connections.Length];
        for (int j = 0; j < connections.Length; j++)
            watchers[j] = new RedisConnectionMultiplexerWatcher(connections[j], _ConnectionBuilder, BaseConfiguration, ClientOptions);

        _ConnectionWatchers = watchers;
    }

    private void CheckIfReady()
    {
        if (_ConnectionWatchers == null || _ConnectionWatchers.Length == 0)
            throw new InvalidOperationException("The pool is not ready");
    }
}
