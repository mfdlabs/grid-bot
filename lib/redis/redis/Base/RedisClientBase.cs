namespace Redis;

using System;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using StackExchange.Redis;

using Hashing;
using Instrumentation;

/// <summary>
/// Base class for Redis Clients.
/// </summary>
/// <typeparam name="TOptions">The options.</typeparam>
public abstract class RedisClientBase<TOptions> : IRedisClient
    where TOptions : RedisClientOptionsBase
{
    private const string _RedisExceptionNoScriptMessageKeyword = "NOSCRIPT";
    private static readonly HashSet<string> _SubscriptionCommands = new()
    {
        "SUBSCRIBE",
        "UNSUBSCRIBE",
        "PSUBSCRIBE",
        "PUNSUBSCRIBE"
    };


    private readonly PerformanceMonitor _PerformanceMonitor;

    /// <summary>
    /// The options for the client.
    /// </summary>
    protected TOptions RedisClientOptions { get; }

    /// <inheritdoc cref="IRedisClient.Refreshed"/>

    public event EventHandler Refreshed;

    /// <inheritdoc cref="IRedisClient.Close"/>
    public abstract void Close();

    /// <inheritdoc cref="IRedisClient.GetSubscriber(string)"/>
    public abstract ISubscriber GetSubscriber(string partitionKey);

    /// <inheritdoc cref="IRedisClient.Refresh(string[])"/>
    public abstract void Refresh(string[] redisEndpoints);

    /// <inheritdoc cref="IRedisClient.GetDatabase(string)"/>
    public abstract IDatabase GetDatabase(string partitionKey);

    /// <inheritdoc cref="IRedisClient.GetServer(string)"/>
    public abstract IServer GetServer(string partitionKey);

    /// <inheritdoc cref="IRedisClient.GetDatabases(IReadOnlyCollection{string})"/>
    public abstract IDictionary<IDatabase, IReadOnlyCollection<string>> GetDatabases(IReadOnlyCollection<string> partitionKeys);

    /// <inheritdoc cref="IRedisClient.GetAllDatabases"/>
    public abstract IReadOnlyCollection<IDatabase> GetAllDatabases();

    /// <inheritdoc cref="IRedisClient.GetAllServers"/>
    public abstract IReadOnlyCollection<IServer> GetAllServers();

    /// <summary>
    /// Construct a new instance of <see cref="RedisClientBase{TOptions}"/>
    /// </summary>
    /// <param name="counterRegistry">The <see cref="ICounterRegistry"/></param>
    /// <param name="performanceMonitorCategory">The performance monitor category.</param>
    /// <param name="redisClientOptions">The <typeparamref name="TOptions"/></param>
    /// <param name="exceptionHandler">An exception handler.</param>
    /// <exception cref="ArgumentNullException"><paramref name="redisClientOptions"/> cannot be null.</exception>
    protected RedisClientBase(ICounterRegistry counterRegistry, string performanceMonitorCategory, TOptions redisClientOptions, Action<Exception> exceptionHandler = null)
    {
        RedisClientOptions = redisClientOptions ?? throw new ArgumentNullException(nameof(redisClientOptions));

        try
        {
            _PerformanceMonitor = new PerformanceMonitor(counterRegistry, performanceMonitorCategory);
        }
        catch (Exception ex)
        {
            exceptionHandler?.Invoke(new Exception("Unable to initialize redis performance monitor.", ex));
        }
    }

    /// <inheritdoc cref="IRedisClient.Execute(string, DatabaseAction)"/>
    public void Execute(string partitionKey, DatabaseAction databaseAction)
    {
        if (string.IsNullOrEmpty(partitionKey))
            throw new ArgumentException("Partition key cannot be null or empty in call to RedisClient.Execute");

        var database = GetDatabase(partitionKey);
        var sw = Stopwatch.StartNew();

        PreDatabaseExecute();

        try
        {
            databaseAction(database);
        }
        catch (Exception)
        {
            OnDatabaseError(database);
            throw;
        }
        finally
        {
            PostDatabaseExecute(sw);
        }
    }

    /// <inheritdoc cref="IRedisClient.Execute{TResult}(string, IPartitionedKeyGenerator, DatabaseAction{TResult})"/>
    public TResult Execute<TResult>(string keyToBePartitioned, IPartitionedKeyGenerator partitionedKeyGenerator, DatabaseAction<TResult> databaseAction)
    {
        var partitionKey = partitionedKeyGenerator.GetPartitionKey(keyToBePartitioned);

        return Execute(partitionKey, databaseAction);
    }

    /// <inheritdoc cref="IRedisClient.Execute{TResult}(string, DatabaseAction{TResult})"/>
    public TResult Execute<TResult>(string partitionKey, DatabaseAction<TResult> databaseAction)
    {
        if (string.IsNullOrEmpty(partitionKey))
            throw new ArgumentException("Partition key cannot be null or empty in call to RedisClient.Execute");

        var database = GetDatabase(partitionKey);
        var sw = Stopwatch.StartNew();

        PreDatabaseExecute();

        try
        {
            return databaseAction(database);
        }
        catch (Exception)
        {
            OnDatabaseError(database);
            throw;
        }
        finally
        {
            PostDatabaseExecute(sw);
        }
    }

    /// <inheritdoc cref="IRedisClient.ExecuteAsync(string, DatabaseActionAsync, CancellationToken)"/>
    public async Task ExecuteAsync(string partitionKey, DatabaseActionAsync databaseAction, CancellationToken cancellationToken)
    {
        if (partitionKey == null) throw new ArgumentNullException(nameof(partitionKey));
        if (databaseAction == null) throw new ArgumentNullException(nameof(databaseAction));

        var database = GetDatabase(partitionKey);
        var sw = Stopwatch.StartNew();

        PreDatabaseExecute();

        try
        {
            await databaseAction(database, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            OnDatabaseError(database);
            throw;
        }
        finally
        {
            PostDatabaseExecute(sw);
        }
    }

    /// <inheritdoc cref="IRedisClient.ExecuteAsync{TResult}(string, DatabaseActionAsync{TResult}, CancellationToken)"/>
    public async Task<TResult> ExecuteAsync<TResult>(string partitionKey, DatabaseActionAsync<TResult> databaseAction, CancellationToken cancellationToken)
    {
        if (partitionKey == null) throw new ArgumentNullException(nameof(partitionKey));
        if (databaseAction == null) throw new ArgumentNullException(nameof(databaseAction));

        var database = GetDatabase(partitionKey);
        var sw = Stopwatch.StartNew();

        PreDatabaseExecute();

        try
        {
            return await databaseAction(database, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            OnDatabaseError(database);
            throw;
        }
        finally
        {
            PostDatabaseExecute(sw);
        }
    }

    /// <inheritdoc cref="IRedisClient.ExecuteMulti{TResult}(IReadOnlyCollection{string}, DatabaseMultiAction{TResult})"/>
    public IEnumerable<(string key, TResult result)> ExecuteMulti<TResult>(IReadOnlyCollection<string> partitionKeys, DatabaseMultiAction<TResult> databaseAction)
    {
        if (partitionKeys != null)
        {
            if (!partitionKeys.Any(k => k == null))
            {
                if (databaseAction == null) throw new ArgumentNullException(nameof(databaseAction));

                var databases = GetDatabases(partitionKeys);
                var allResults = new List<(string key, TResult result)>(partitionKeys.Count);
                foreach (var databaseBucketMapping in databases)
                {
                    var sw = Stopwatch.StartNew();
                    PreDatabaseExecute();
                    IEnumerable<TResult> actionResults;
                    try
                    {
                        actionResults = databaseAction(databaseBucketMapping.Key, databaseBucketMapping.Value);
                    }
                    catch (Exception)
                    {
                        OnDatabaseError(databaseBucketMapping.Key);
                        throw;
                    }
                    finally
                    {
                        PostDatabaseExecute(sw);
                    }

                    using var keys = databaseBucketMapping.Value.AsEnumerable().GetEnumerator();
                    using var values = actionResults.GetEnumerator();

                    while (keys.MoveNext() && values.MoveNext())
                        allResults.Add((keys.Current, values.Current));
                }

                return allResults;
            }
        }

        throw new ArgumentNullException(nameof(partitionKeys));
    }

    /// <inheritdoc cref="IRedisClient.ExecuteMulti{TResult}(IReadOnlyCollection{string}, DatabaseMultiActionWithKeys{TResult})"/>
    public IEnumerable<(string key, TResult result)> ExecuteMulti<TResult>(IReadOnlyCollection<string> partitionKeys, DatabaseMultiActionWithKeys<TResult> databaseAction)
    {
        if (partitionKeys != null)
        {
            if (!partitionKeys.Any((string k) => k == null))
            {
                if (databaseAction == null) throw new ArgumentNullException(nameof(databaseAction));

                var databases = GetDatabases(partitionKeys);
                var allResults = new List<(string key, TResult result)>(partitionKeys.Count);
                foreach (var databaseBucketMapping in databases)
                {
                    var sw = Stopwatch.StartNew();

                    PreDatabaseExecute();

                    IEnumerable<string> keys;
                    IEnumerable<TResult> values;
                    try
                    {
                        (keys, values) = databaseAction(databaseBucketMapping.Key, databaseBucketMapping.Value);
                    }
                    catch (Exception)
                    {
                        OnDatabaseError(databaseBucketMapping.Key);
                        throw;
                    }
                    finally
                    {
                        PostDatabaseExecute(sw);
                    }

                    using var keyEnu = keys.GetEnumerator();
                    using var valueEnu = values.GetEnumerator();

                    while (keyEnu.MoveNext() && valueEnu.MoveNext())
                        allResults.Add((keyEnu.Current, valueEnu.Current));
                }

                return allResults;
            }
        }

        throw new ArgumentNullException(nameof(partitionKeys));
    }

    /// <inheritdoc cref="IRedisClient.ExecuteMultiAsync{TResult}(IReadOnlyCollection{string}, DatabaseMultiActionAsync{TResult}, CancellationToken)"/>
    public async Task<IEnumerable<(string key, TResult result)>> ExecuteMultiAsync<TResult>(IReadOnlyCollection<string> partitionKeys, DatabaseMultiActionAsync<TResult> databaseAction, CancellationToken cancellationToken)
    {
        if (partitionKeys != null)
        {
            if (!partitionKeys.Any(k => k == null))
            {
                if (databaseAction == null) throw new ArgumentNullException(nameof(databaseAction));

                var databases = GetDatabases(partitionKeys);
                var allResults = new List<(string key, TResult result)>(partitionKeys.Count);
                foreach (var databaseBucketMapping in databases)
                {
                    var sw = Stopwatch.StartNew();

                    PreDatabaseExecute();

                    IEnumerable<TResult> actionResults;
                    try
                    {
                        actionResults = await databaseAction(databaseBucketMapping.Key, databaseBucketMapping.Value, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        OnDatabaseError(databaseBucketMapping.Key);
                        throw;
                    }
                    finally
                    {
                        PostDatabaseExecute(sw);
                    }

                    using var keys = databaseBucketMapping.Value.AsEnumerable().GetEnumerator();
                    using var values = actionResults.GetEnumerator();

                    while (keys.MoveNext() && values.MoveNext())
                        allResults.Add((keys.Current, values.Current));
                }

                return allResults;
            }
        }

        throw new ArgumentNullException(nameof(partitionKeys));
    }

    /// <inheritdoc cref="IRedisClient.ExecuteMulti(IReadOnlyCollection{string}, DatabaseMultiAction)"/>
    public void ExecuteMulti(IReadOnlyCollection<string> partitionKeys, DatabaseMultiAction databaseAction)
    {
        if (partitionKeys != null)
        {
            if (!partitionKeys.Any(k => k == null))
            {
                if (databaseAction == null) throw new ArgumentNullException(nameof(databaseAction));

                var databases = GetDatabases(partitionKeys);
                foreach (var databaseBucketMapping in databases)
                {
                    var sw = Stopwatch.StartNew();

                    PreDatabaseExecute();

                    try
                    {
                        databaseAction(databaseBucketMapping.Key, databaseBucketMapping.Value);
                    }
                    catch (Exception)
                    {
                        OnDatabaseError(databaseBucketMapping.Key);
                        throw;
                    }
                    finally
                    {
                        PostDatabaseExecute(sw);
                    }
                }

                return;
            }
        }

        throw new ArgumentNullException(nameof(partitionKeys));
    }

    /// <inheritdoc cref="IRedisClient.ExecuteMultiAsync(IReadOnlyCollection{string}, DatabaseMultiActionAsync, CancellationToken)"/>
    public async Task ExecuteMultiAsync(IReadOnlyCollection<string> partitionKeys, DatabaseMultiActionAsync databaseAction, CancellationToken cancellationToken)
    {
        if (partitionKeys != null)
        {
            if (!partitionKeys.Any((string k) => k == null))
            {
                if (databaseAction == null) throw new ArgumentNullException(nameof(databaseAction));

                cancellationToken.ThrowIfCancellationRequested();

                var databases = GetDatabases(partitionKeys);
                foreach (var databaseBucketMapping in databases)
                {
                    var sw = Stopwatch.StartNew();

                    PreDatabaseExecute();

                    try
                    {
                        await databaseAction(databaseBucketMapping.Key, databaseBucketMapping.Value, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        OnDatabaseError(databaseBucketMapping.Key);
                        throw;
                    }
                    finally
                    {
                        PostDatabaseExecute(sw);
                    }

                }

                return;
            }
        }

        throw new ArgumentNullException(nameof(partitionKeys));
    }

    /// <inheritdoc cref="IRedisClient.ExecuteOnNodes(DatabaseAction, Func{IDatabase, bool})"/>
    public void ExecuteOnNodes(DatabaseAction databaseAction, Func<IDatabase, bool> shouldExecuteOnNode = null)
    {
        if (databaseAction == null) throw new ArgumentNullException(nameof(databaseAction));

        var allDatabases = GetAllDatabases();
        foreach (var database in allDatabases)
        {
            if (shouldExecuteOnNode == null || shouldExecuteOnNode(database))
            {
                var sw = Stopwatch.StartNew();

                PreDatabaseExecute();

                try
                {
                    databaseAction(database);
                }
                catch (Exception)
                {
                    OnDatabaseError(database);
                    throw;
                }
                finally
                {
                    PostDatabaseExecute(sw);
                }
            }
        }
    }

    /// <inheritdoc cref="IRedisClient.ExecuteOnNodesAsync(DatabaseActionAsync, Func{IDatabase, bool}, CancellationToken)"/>
    public async Task ExecuteOnNodesAsync(DatabaseActionAsync databaseAction, Func<IDatabase, bool> shouldExecuteOnNode = null, CancellationToken cancellationToken = default(CancellationToken))
    {
        if (databaseAction == null) throw new ArgumentNullException(nameof(databaseAction));

        cancellationToken.ThrowIfCancellationRequested();

        var allDatabases = GetAllDatabases();
        var tasks = new List<Task>(allDatabases.Count);

        foreach (var database in allDatabases)
            if (shouldExecuteOnNode == null || shouldExecuteOnNode(database))
                tasks.Add(DatabaseActionWrapperAsync(database));

        await Task.WhenAll(tasks).ConfigureAwait(false);

        async Task DatabaseActionWrapperAsync(IDatabase database) => await databaseAction(database, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc cref="IRedisClient.ExecuteOnNodes{TResult}(DatabaseAction{TResult}, Func{IDatabase, bool})"/>
    public IEnumerable<TResult> ExecuteOnNodes<TResult>(DatabaseAction<TResult> databaseAction, Func<IDatabase, bool> shouldExecuteOnNode = null)
    {
        if (databaseAction == null) throw new ArgumentNullException(nameof(databaseAction));

        var allDatabases = GetAllDatabases();
        var allResults = new List<TResult>();

        foreach (var database in allDatabases)
        {
            if (shouldExecuteOnNode == null || shouldExecuteOnNode(database))
            {
                var sw = Stopwatch.StartNew();

                PreDatabaseExecute();

                try
                {
                    allResults.Add(databaseAction(database));
                }
                catch (Exception)
                {
                    OnDatabaseError(database);
                    throw;
                }
                finally
                {
                    PostDatabaseExecute(sw);
                }
            }
        }

        return allResults;
    }

    /// <inheritdoc cref="IRedisClient.ExecuteOnNodesAsync{TResult}(DatabaseActionAsync{TResult}, Func{IDatabase, bool}, CancellationToken)"/>
    public async Task<IEnumerable<TResult>> ExecuteOnNodesAsync<TResult>(DatabaseActionAsync<TResult> databaseAction, Func<IDatabase, bool> shouldExecuteOnNode = null, CancellationToken cancellationToken = default(CancellationToken))
    {
        if (databaseAction == null) throw new ArgumentNullException(nameof(databaseAction));

        cancellationToken.ThrowIfCancellationRequested();

        var allDatabases = GetAllDatabases();
        var tasks = new List<Task<TResult>>(allDatabases.Count);

        async Task<TResult> DatabaseActionWrapperAsync(IDatabase database)
        {
            return await databaseAction(database, cancellationToken).ConfigureAwait(false);
        }

        foreach (var database in allDatabases)
            if (shouldExecuteOnNode == null || shouldExecuteOnNode(database))
                tasks.Add(DatabaseActionWrapperAsync(database));

        return await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <inheritdoc cref="IRedisClient.ExecuteScript(IDatabase, string, RedisKey[], RedisValue[], CommandFlags)"/>
    public void ExecuteScript(IDatabase database, string script, RedisKey[] keys = null, RedisValue[] values = null, CommandFlags flags = CommandFlags.None)
        => database.ScriptEvaluate(script, keys, values, flags);

    /// <inheritdoc cref="IRedisClient.ExecuteScriptAsync(IDatabase, string, RedisKey[], RedisValue[], CommandFlags, CancellationToken)"/>
    public Task ExecuteScriptAsync(IDatabase database, string script, RedisKey[] keys = null, RedisValue[] values = null, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default(CancellationToken))
    {
        cancellationToken.ThrowIfCancellationRequested();

        return database.ScriptEvaluateAsync(script, keys, values, flags);
    }

    /// <inheritdoc cref="IRedisClient.ExecuteScript{TResult}(IDatabase, ConvertRedisResult{TResult}, string, RedisKey[], RedisValue[], CommandFlags)"/>
    public TResult ExecuteScript<TResult>(IDatabase database, ConvertRedisResult<TResult> convertRedisResult, string script, RedisKey[] keys = null, RedisValue[] values = null, CommandFlags flags = CommandFlags.None)
        => convertRedisResult(database.ScriptEvaluate(script, keys, values, flags));

    /// <inheritdoc cref="IRedisClient.ExecuteScriptAsync{TResult}(IDatabase, ConvertRedisResult{TResult}, string, RedisKey[], RedisValue[], CommandFlags, CancellationToken)"/>
    public async Task<TResult> ExecuteScriptAsync<TResult>(IDatabase database, ConvertRedisResult<TResult> convertRedisResult, string script, RedisKey[] keys = null, RedisValue[] values = null, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default(CancellationToken))
    {
        cancellationToken.ThrowIfCancellationRequested();

        return convertRedisResult(await database.ScriptEvaluateAsync(script, keys, values, flags).ConfigureAwait(false));
    }

    /// <inheritdoc cref="IRedisClient.ExecuteLoadedScript(IDatabase, string, byte[], RedisKey[], RedisValue[], CommandFlags)"/>
    public void ExecuteLoadedScript(IDatabase database, string script, byte[] scriptHash, RedisKey[] keys = null, RedisValue[] values = null, CommandFlags flags = CommandFlags.None)
        => ExecuteLoadedScript(database, s => s, script, scriptHash, keys, values, flags);

    /// <inheritdoc cref="IRedisClient.ExecuteLoadedScriptAsync(IDatabase, string, byte[], RedisKey[], RedisValue[], CommandFlags, CancellationToken)"/>
    public Task ExecuteLoadedScriptAsync(IDatabase database, string script, byte[] scriptHash, RedisKey[] keys = null, RedisValue[] values = null, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default(CancellationToken))
    {
        cancellationToken.ThrowIfCancellationRequested();

        return ExecuteLoadedScriptAsync(database, s => s, script, scriptHash, keys, values, flags, cancellationToken);
    }

    /// <inheritdoc cref="IRedisClient.ExecuteLoadedScript{TResult}(IDatabase, ConvertRedisResult{TResult}, string, byte[], RedisKey[], RedisValue[], CommandFlags)"/>
    public TResult ExecuteLoadedScript<TResult>(IDatabase database, ConvertRedisResult<TResult> convertRedisResult, string script, byte[] scriptHash, RedisKey[] keys = null, RedisValue[] values = null, CommandFlags flags = CommandFlags.None)
    {
        try
        {
            return convertRedisResult(database.ScriptEvaluate(scriptHash, keys, values, flags));
        }
        catch (RedisServerException ex)
        {
            if (!ex.Message.Contains(_RedisExceptionNoScriptMessageKeyword))
                throw;

            LoadScript(database, scriptHash, script);
            return convertRedisResult(database.ScriptEvaluate(scriptHash, keys, values, flags));
        }
    }

    /// <inheritdoc cref="IRedisClient.ExecuteLoadedScriptAsync{TResult}(IDatabase, ConvertRedisResult{TResult}, string, byte[], RedisKey[], RedisValue[], CommandFlags, CancellationToken)"/>
    public async Task<TResult> ExecuteLoadedScriptAsync<TResult>(IDatabase database, ConvertRedisResult<TResult> convertRedisResult, string script, byte[] scriptHash, RedisKey[] keys = null, RedisValue[] values = null, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default(CancellationToken))
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            return convertRedisResult(await database.ScriptEvaluateAsync(scriptHash, keys, values, flags));
        }
        catch (RedisServerException ex)
        {
            if (!ex.Message.Contains(_RedisExceptionNoScriptMessageKeyword))
                throw;

            LoadScript(database, scriptHash, script);
            return convertRedisResult(await database.ScriptEvaluateAsync(scriptHash, keys, values, flags));
        }
    }

    /// <inheritdoc cref="IRedisClient.GetScriptHash(string)"/>
    public byte[] GetScriptHash(string script)
        => LuaScriptHasher.GetScriptHash(script);

    /// <inheritdoc cref="IRedisClient.Refresh(RedisEndpoints)"/>
    public void Refresh(RedisEndpoints redisEndpoints)
        => Refresh(redisEndpoints.Endpoints.ToArray());

    /// <inheritdoc cref="IRedisClient.PingAllDatabases"/>
    public void PingAllDatabases()
        => Parallel.ForEach(GetAllDatabases(), db => db.Ping());

    /// <inheritdoc cref="IRedisClient.GetAllSubscribers"/>
    public abstract IReadOnlyCollection<ISubscriber> GetAllSubscribers();

    /// <summary>
    /// Connect the multiplexer.
    /// </summary>
    /// <param name="redisEndpoint">The Redis endpoint.</param>
    /// <returns>The connection multiplexer.</returns>
    protected Task<IConnectionMultiplexer> ConnectMultiplexerAsync(string redisEndpoint)
    {
        var opts = GetConfigurationOptions(redisEndpoint);

        return (RedisClientOptions.ConnectionBuilder ?? new DefaultConnectionBuilder()).CreateConnectionMultiplexerAsync(opts);
    }

    /// <summary>
    /// Get the configuration options for the specified endpoint.
    /// </summary>
    /// <param name="redisEndpoint">The Redis endpoint.</param>
    /// <returns>The connection multiplexer.</returns>
    protected ConfigurationOptions GetConfigurationOptions(string redisEndpoint)
    {
        var opts = ConfigurationOptions.Parse(redisEndpoint);

        opts.ConnectTimeout = (int)RedisClientOptions.ConnectTimeout().TotalMilliseconds;
        opts.ResponseTimeout = (int)RedisClientOptions.ResponseTimeout().TotalMilliseconds;
        opts.AbortOnConnectFail = false;
        opts.ReconnectRetryPolicy = RedisClientOptions.ReconnectRetryPolicy;

        if (RedisClientOptions.SyncTimeout != null)
            opts.SyncTimeout = (int)RedisClientOptions.SyncTimeout.Value.TotalMilliseconds;

        if (RedisClientOptions.DisableSubscriptions)
            opts.CommandMap = CommandMap.Create(_SubscriptionCommands, false);

        return opts;
    }

    /// <summary>
    /// Connect the multiplexer in sync.
    /// </summary>
    /// <param name="redisEndpoint">The Redis endpoint.</param>
    /// <returns>The connection multiplexer.</returns>
    protected IConnectionMultiplexer ConnectMultiplexer(string redisEndpoint)
        => Task.Run(() => ConnectMultiplexerAsync(redisEndpoint)).GetAwaiter().GetResult();

    /// <summary>
    /// Executed when the client is refreshed.
    /// </summary>
    protected void OnRefreshed()
        => Refreshed?.Invoke(this, EventArgs.Empty);

    private void PreDatabaseExecute()
        => _PerformanceMonitor?.OutstandingRequestCount.Increment();

    private void OnDatabaseError(IDatabase database)
    {
        _PerformanceMonitor?.ErrorsPerSecond.Increment();
        _PerformanceMonitor?.GetPerEndpointErrorCounter(database.Multiplexer.GetIpPortCombo()).Increment();
    }

    private void PostDatabaseExecute(Stopwatch stopWatch)
    {
        stopWatch.Stop();

        if (_PerformanceMonitor == null)
            return;

        _PerformanceMonitor.AverageResponseTime.Sample(stopWatch.Elapsed.TotalMilliseconds);
        _PerformanceMonitor.RequestsPerSecond.Increment();
        _PerformanceMonitor.OutstandingRequestCount.Decrement();
    }

    private void LoadScript(IDatabase database, byte[] scriptHash, string script)
    {
        if (!GetServerFromMultiplexer(database.Multiplexer).ScriptLoad(script).SequenceEqual(scriptHash))
            throw new ArgumentException("scriptHash is not correct for the script.");
    }

    /// <summary>
    /// Get an <see cref="IServer"/> from an <see cref="IConnectionMultiplexer"/>
    /// </summary>
    /// <param name="connectionMultiplexer">The <see cref="IConnectionMultiplexer"/></param>
    /// <returns>The <see cref="IServer"/></returns>
    /// <exception cref="ArgumentNullException"><paramref name="connectionMultiplexer"/> cannot be null.</exception>
    public static IServer GetServerFromMultiplexer(IConnectionMultiplexer connectionMultiplexer)
    {
        if (connectionMultiplexer == null)
            throw new ArgumentNullException(nameof(connectionMultiplexer));

        var endPoint = connectionMultiplexer.GetEndPoints()[0];
        return connectionMultiplexer.GetServer(endPoint);
    }
}
