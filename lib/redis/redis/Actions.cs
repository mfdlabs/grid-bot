namespace Redis;

using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using StackExchange.Redis;

/// <summary>
/// Perform a default action on the database.
/// </summary>
/// <param name="database">The database.</param>
public delegate void DatabaseAction(IDatabase database);

/// <summary>
/// Perform an action that expects a result on the database.
/// </summary>
/// <typeparam name="TResult">The type of the result.</typeparam>
/// <param name="database">The database.</param>
/// <returns>The expected result.</returns>
public delegate TResult DatabaseAction<out TResult>(IDatabase database);

/// <summary>
/// Perform a default action on the database asynchronously.
/// </summary>
/// <param name="database">The database.</param>
/// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
/// <returns>An awaitable task.</returns>
public delegate Task DatabaseActionAsync(IDatabase database, CancellationToken cancellationToken = default);

/// <summary>
/// Perform an action that expects a result on the database asynchronously.
/// </summary>
/// <typeparam name="TResult">The type of the result.</typeparam>
/// <param name="database">The database.</param>
/// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
/// <returns>The expected result.</returns>
public delegate Task<TResult> DatabaseActionAsync<TResult>(IDatabase database, CancellationToken cancellationToken = default);

/// <summary>
/// Perform multiple actions on the specified keys.
/// </summary>
/// <param name="database">The database.</param>
/// <param name="keys">A list of keys to execute the action on.</param>
public delegate void DatabaseMultiAction(IDatabase database, IReadOnlyCollection<string> keys);

/// <summary>
/// Perform a multi action that expects results on the specified keys.
/// </summary>
/// <typeparam name="TResult">The type of the result.</typeparam>
/// <param name="database">The database.</param>
/// <param name="keys">A list of keys to execute the action on.</param>
/// <returns>The result of each key action.</returns>
public delegate IEnumerable<TResult> DatabaseMultiAction<out TResult>(IDatabase database, IReadOnlyCollection<string> keys);

/// <summary>
/// Perform multiple actions on the specified keys asynchronously.
/// </summary>
/// <param name="database">The database.</param>
/// <param name="keys">A list of keys to execute the action on.</param>
/// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
/// <returns>An awaitable task.</returns>
public delegate Task DatabaseMultiActionAsync(IDatabase database, IReadOnlyCollection<string> keys, CancellationToken cancellationToken = default);

/// <summary>
/// Perform a multi action that expects results on the specified keys asynchronously.
/// </summary>
/// <typeparam name="TResult">The type of the result.</typeparam>
/// <param name="database">The database.</param>
/// <param name="keys">A list of keys to execute the action on.</param>
/// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
/// <returns>The result of each key action.</returns>
public delegate Task<IEnumerable<TResult>> DatabaseMultiActionAsync<TResult>(IDatabase database, IReadOnlyCollection<string> keys, CancellationToken cancellationToken = default);

/// <summary>
/// Convert the specified <see cref="RedisResult"/> to <typeparamref name="TResult"/>
/// </summary>
/// <typeparam name="TResult">The type of the result.</typeparam>
/// <param name="redisResult">The result.</param>
/// <returns>The converted result.</returns>
public delegate TResult ConvertRedisResult<out TResult>(RedisResult redisResult);

/// <summary>
/// Perform multiple actions on the specified keys returning the keys and their results.
/// </summary>
/// <typeparam name="TResult">The type of the result.</typeparam>
/// <param name="database">The database.</param>
/// <param name="partitionKeys">The keys.</param>
/// <returns>A value tuple of the keys and results.</returns>
public delegate (IEnumerable<string> keys, IEnumerable<TResult> results) DatabaseMultiActionWithKeys<TResult>(IDatabase database, IReadOnlyCollection<string> partitionKeys);