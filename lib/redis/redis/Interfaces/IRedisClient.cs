namespace Redis;

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using StackExchange.Redis;

using Hashing;

/// <summary>
/// Base interface for a Redis client.
/// </summary>
public interface IRedisClient
{
    /// <summary>
    /// Execute a database action on the following key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="databaseAction">The action.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="key"/> cannot be null.
    /// - <paramref name="databaseAction"/> cannot be null.
    /// </exception>
    void Execute(string key, DatabaseAction databaseAction);

    /// <summary>
    /// Execute a database action on the following partitioned key.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="keyToBePartitioned">The key.</param>
    /// <param name="partitionedKeyGenerator">The generator for partitioned keys.</param>
    /// <param name="databaseAction">The action.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="keyToBePartitioned"/> cannot be null.
    /// - <paramref name="partitionedKeyGenerator"/> cannot be null.
    /// - <paramref name="databaseAction"/> cannot be null.
    /// </exception>
    TResult Execute<TResult>(string keyToBePartitioned, IPartitionedKeyGenerator partitionedKeyGenerator, DatabaseAction<TResult> databaseAction);

    /// <summary>
    /// Execute a database action that expects a result on the following key.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="key">The key.</param>
    /// <param name="databaseAction">The action.</param>
    /// <returns>The expected result.</returns>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="key"/> cannot be null.
    /// - <paramref name="databaseAction"/> cannot be null.
    /// </exception>
    TResult Execute<TResult>(string key, DatabaseAction<TResult> databaseAction);

    /// <summary>
    /// Execute a database action on the following key asynchronously.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="databaseAction">The action.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An awaitable task.</returns>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="key"/> cannot be null.
    /// - <paramref name="databaseAction"/> cannot be null.
    /// </exception>
    Task ExecuteAsync(string key, DatabaseActionAsync databaseAction, CancellationToken cancellationToken);

    /// <summary>
    /// Execute a database action that expects a result on the following key asynchronously.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="key">The key.</param>
    /// <param name="databaseAction">The action.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The expected result.</returns>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="key"/> cannot be null.
    /// - <paramref name="databaseAction"/> cannot be null.
    /// </exception>
    Task<TResult> ExecuteAsync<TResult>(string key, DatabaseActionAsync<TResult> databaseAction, CancellationToken cancellationToken);

    /// <summary>
    /// Execute a database action on all the specified keys.
    /// </summary>
    /// <param name="partitionKeys">The partition keys.</param>
    /// <param name="databaseAction">The database action.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="partitionKeys"/> cannot be null.
    /// - <paramref name="databaseAction"/> cannot be null.
    /// </exception>
    void ExecuteMulti(IReadOnlyCollection<string> partitionKeys, DatabaseMultiAction databaseAction);

    /// <summary>
    /// Execute a database action on all the specified keys returning the results from each execution.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="partitionKeys">The partition keys.</param>
    /// <param name="databaseAction">The database action.</param>
    /// <returns>An enumerable array of key and result.</returns>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="partitionKeys"/> cannot be null.
    /// - <paramref name="databaseAction"/> cannot be null.
    /// </exception>
    IEnumerable<(string key, TResult result)> ExecuteMulti<TResult>(IReadOnlyCollection<string> partitionKeys, DatabaseMultiAction<TResult> databaseAction);

    /// <summary>
    /// Execute a database action on all the specified keys returning the results from each execution.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="partitionKeys">The partition keys.</param>
    /// <param name="databaseAction">The database action.</param>
    /// <returns>An enumerable array of key and result.</returns>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="partitionKeys"/> cannot be null.
    /// - <paramref name="databaseAction"/> cannot be null.
    /// </exception>
    IEnumerable<(string key, TResult result)> ExecuteMulti<TResult>(IReadOnlyCollection<string> partitionKeys, DatabaseMultiActionWithKeys<TResult> databaseAction);

    /// <summary>
    /// Execute a database action on all the specified keys returning the results from each execution asynchronously.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="partitionKeys">The partition keys.</param>
    /// <param name="databaseAction">The database action.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An enumerable array of key and result.</returns>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="partitionKeys"/> cannot be null.
    /// - <paramref name="databaseAction"/> cannot be null.
    /// </exception>
    Task<IEnumerable<(string key, TResult result)>> ExecuteMultiAsync<TResult>(IReadOnlyCollection<string> partitionKeys, DatabaseMultiActionAsync<TResult> databaseAction, CancellationToken cancellationToken);

    /// <summary>
    /// Execute a database action on all the specified keys asynchronously.
    /// </summary>
    /// <param name="partitionKeys">The partition keys.</param>
    /// <param name="databaseAction">The database action.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An enumerable array of key and result.</returns>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="partitionKeys"/> cannot be null.
    /// - <paramref name="databaseAction"/> cannot be null.
    /// </exception>
    Task ExecuteMultiAsync(IReadOnlyCollection<string> partitionKeys, DatabaseMultiActionAsync databaseAction, CancellationToken cancellationToken);

    /// <summary>
    /// Execute the specified action on all nodes in the redis endpoints.
    /// </summary>
    /// <param name="databaseAction">The action to execute.</param>
    /// <param name="shouldExecuteOnNode">An optional function to determine if the node is suitable to execute on.</param>
    /// <exception cref="ArgumentNullException"><paramref name="databaseAction"/> cannot be null.</exception>
    void ExecuteOnNodes(DatabaseAction databaseAction, Func<IDatabase, bool> shouldExecuteOnNode = null);

    /// <summary>
    /// Execute the specified action on all nodes in the redis endpoints asynchronously.
    /// </summary>
    /// <param name="databaseAction">The action to execute.</param>
    /// <param name="shouldExecuteOnNode">An optional function to determine if the node is suitable to execute on.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="ArgumentNullException"><paramref name="databaseAction"/> cannot be null.</exception>
    Task ExecuteOnNodesAsync(DatabaseActionAsync databaseAction, Func<IDatabase, bool> shouldExecuteOnNode = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute the specified action on all nodes in the redis endpoints and return the result from each execution.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="databaseAction">The action to execute.</param>
    /// <param name="shouldExecuteOnNode">An optional function to determine if the node is suitable to execute on.</param>
    /// <exception cref="ArgumentNullException"><paramref name="databaseAction"/> cannot be null.</exception>
    IEnumerable<TResult> ExecuteOnNodes<TResult>(DatabaseAction<TResult> databaseAction, Func<IDatabase, bool> shouldExecuteOnNode = null);

    /// <summary>
    /// Execute the specified action on all nodes in the redis endpoints and return the result from each execution asynchronously.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="databaseAction">The action to execute.</param>
    /// <param name="shouldExecuteOnNode">An optional function to determine if the node is suitable to execute on.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="ArgumentNullException"><paramref name="databaseAction"/> cannot be null.</exception>
    Task<IEnumerable<TResult>> ExecuteOnNodesAsync<TResult>(DatabaseActionAsync<TResult> databaseAction, Func<IDatabase, bool> shouldExecuteOnNode = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a script on the specified database.
    /// </summary>
    /// <param name="database">The database.</param>
    /// <param name="script">The script.</param>
    /// <param name="keys">The keys.s</param>
    /// <param name="values">The values.</param>
    /// <param name="flags">The flags.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="database"/> cannot be null.
    /// - <paramref name="script"/> cannot be null.
    /// </exception>
    void ExecuteScript(IDatabase database, string script, RedisKey[] keys = null, RedisValue[] values = null, CommandFlags flags = CommandFlags.None);

    /// <summary>
    /// Execute a script on the specified database asynchronously.
    /// </summary>
    /// <param name="database">The database.</param>
    /// <param name="script">The script.</param>
    /// <param name="keys">The keys.s</param>
    /// <param name="values">The values.</param>
    /// <param name="flags">The flags.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="database"/> cannot be null.
    /// - <paramref name="script"/> cannot be null.
    /// </exception>
    Task ExecuteScriptAsync(IDatabase database, string script, RedisKey[] keys = null, RedisValue[] values = null, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a script on the specified database expecting a result.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="database">The database.</param>
    /// <param name="convertRedisResult">The method to convert the result.</param>
    /// <param name="script">The script.</param>
    /// <param name="keys">The keys.s</param>
    /// <param name="values">The values.</param>
    /// <param name="flags">The flags.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="database"/> cannot be null.
    /// - <paramref name="script"/> cannot be null.
    /// </exception>
    TResult ExecuteScript<TResult>(IDatabase database, ConvertRedisResult<TResult> convertRedisResult, string script, RedisKey[] keys = null, RedisValue[] values = null, CommandFlags flags = 0);

    /// <summary>
    /// Execute a script on the specified database asynchronously.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="database">The database.</param>
    /// <param name="convertRedisResult">The method to convert the result.</param>
    /// <param name="script">The script.</param>
    /// <param name="keys">The keys.s</param>
    /// <param name="values">The values.</param>
    /// <param name="flags">The flags.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="database"/> cannot be null.
    /// - <paramref name="script"/> cannot be null.
    /// </exception>
    Task<TResult> ExecuteScriptAsync<TResult>(IDatabase database, ConvertRedisResult<TResult> convertRedisResult, string script, RedisKey[] keys = null, RedisValue[] values = null, CommandFlags flags = 0, CancellationToken cancellationToken = default(CancellationToken));

    /// <summary>
    /// Execute a loaded script on the specified database.
    /// </summary>
    /// <param name="database">The database.</param>
    /// <param name="script">The script.</param>
    /// <param name="scriptHash">The script hash.</param>
    /// <param name="keys">The keys.s</param>
    /// <param name="values">The values.</param>
    /// <param name="flags">The flags.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="database"/> cannot be null.
    /// - <paramref name="script"/> cannot be null.
    /// </exception>
    void ExecuteLoadedScript(IDatabase database, string script, byte[] scriptHash, RedisKey[] keys = null, RedisValue[] values = null, CommandFlags flags = CommandFlags.None);

    /// <summary>
    /// Execute a loaded script on the specified database asynchronously.
    /// </summary>
    /// <param name="database">The database.</param>
    /// <param name="script">The script.</param>
    /// <param name="scriptHash">The script hash.</param>
    /// <param name="keys">The keys.s</param>
    /// <param name="values">The values.</param>
    /// <param name="flags">The flags.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="database"/> cannot be null.
    /// - <paramref name="script"/> cannot be null.
    /// </exception>
    Task ExecuteLoadedScriptAsync(IDatabase database, string script, byte[] scriptHash, RedisKey[] keys = null, RedisValue[] values = null, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a loaded script on the specified database expecting a result.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="database">The database.</param>
    /// <param name="convertRedisResult">The method to convert the result.</param>
    /// <param name="script">The script.</param>
    /// <param name="scriptHash">The script hash.</param>
    /// <param name="keys">The keys.s</param>
    /// <param name="values">The values.</param>
    /// <param name="flags">The flags.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="database"/> cannot be null.
    /// - <paramref name="script"/> cannot be null.
    /// </exception>
    TResult ExecuteLoadedScript<TResult>(IDatabase database, ConvertRedisResult<TResult> convertRedisResult, string script, byte[] scriptHash, RedisKey[] keys = null, RedisValue[] values = null, CommandFlags flags = CommandFlags.None);

    /// <summary>
    /// Execute a loaded script on the specified database asynchronously.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="database">The database.</param>
    /// <param name="convertRedisResult">The method to convert the result.</param>
    /// <param name="script">The script.</param>
    /// <param name="scriptHash">The script hash.</param>
    /// <param name="keys">The keys.s</param>
    /// <param name="values">The values.</param>
    /// <param name="flags">The flags.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="database"/> cannot be null.
    /// - <paramref name="script"/> cannot be null.
    /// </exception>
    Task<TResult> ExecuteLoadedScriptAsync<TResult>(IDatabase database, ConvertRedisResult<TResult> convertRedisResult, string script, byte[] scriptHash, RedisKey[] keys = null, RedisValue[] values = null, CommandFlags flags = CommandFlags.None, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the hash for a script.
    /// </summary>
    /// <param name="script">The script.</param>
    /// <returns>The script hash</returns>
    [Obsolete("Use LuaScriptHasher class")]
    byte[] GetScriptHash(string script);

    /// <summary>
    /// Get a subscriber for the specified key.
    /// </summary>
    /// <param name="partitionKey">The partition key.</param>
    /// <returns>The subscriber.</returns>
    ISubscriber GetSubscriber(string partitionKey);

    /// <summary>
    /// Refresh the client.
    /// </summary>
    /// <param name="redisEndpoints">The new redis endpoints.</param>
    void Refresh(RedisEndpoints redisEndpoints);

    /// <summary>
    /// Refresh the client.
    /// </summary>
    /// <param name="redisEndpoints">The new redis endpoints.</param>
    void Refresh(string[] redisEndpoints);

    /// <summary>
    /// Get a server for the specified key.
    /// </summary>
    /// <param name="partitionKey">The partition key.</param>
    /// <returns>The server.</returns>
    IServer GetServer(string partitionKey);

    /// <summary>
    /// Get all servers.
    /// </summary>
    /// <returns>The servers.</returns>
    IReadOnlyCollection<IServer> GetAllServers();

    /// <summary>
    /// Get a database for the specified key.
    /// </summary>
    /// <param name="partitionKey">The partition key.</param>
    /// <returns>The database.</returns>
    IDatabase GetDatabase(string partitionKey);

    /// <summary>
    /// Get all databases for the specified keys.
    /// </summary>
    /// <param name="partitionKeys">The partition keys</param>
    /// <returns>The databases.</returns>
    IDictionary<IDatabase, IReadOnlyCollection<string>> GetDatabases(IReadOnlyCollection<string> partitionKeys);

    /// <summary>
    /// Get all databases.
    /// </summary>
    /// <returns>The databases.</returns>
    IReadOnlyCollection<IDatabase> GetAllDatabases();

    /// <summary>
    /// Ping all databases.
    /// </summary>
    void PingAllDatabases();

    /// <summary>
    /// Get all subscribers.
    /// </summary>
    /// <returns>The subscribers.</returns>
    IReadOnlyCollection<ISubscriber> GetAllSubscribers();

    /// <summary>
    /// Close the client.
    /// </summary>
    void Close();

    /// <summary>
    /// An event executed when the client is refreshed.
    /// </summary>
    event EventHandler Refreshed;
}