﻿namespace Redis;

using System;
using System.Net;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

using Logging;
using ServiceDiscovery;

/// <summary>
/// Redis Client provider for discovering Redis Servers.
/// </summary>
public class DiscoveringRedisClientProvider : IRedisClientProvider
{
    private readonly ILogger _Logger;
    private readonly IServiceResolver _ServiceResolver;
    private readonly Lazy<IRedisClient> _RedisClient;

    /// <inheritdoc cref="IRedisClientProvider.Client"/>
    public IRedisClient Client => _RedisClient.Value;

    /// <summary>
    /// Construct a new instance of <see cref="DiscoveringRedisClientProvider"/>
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="serviceResolver">The <see cref="IServiceResolver"/></param>
    /// <param name="useConnectionPooling">Should use pooling</param>
    /// <param name="redisPooledClientOptions">The <see cref="RedisPooledClientOptions"/></param>
    /// <param name="clientOptions">The <see cref="RedisClientOptions"/></param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="logger"/> cannot be null.
    /// - <paramref name="serviceResolver"/> cannot be null.
    /// </exception>
    public DiscoveringRedisClientProvider(
        ILogger logger, 
        IServiceResolver serviceResolver, 
        bool useConnectionPooling = false, 
        RedisPooledClientOptions redisPooledClientOptions = null, 
        RedisClientOptions clientOptions = null
    )
    {
        _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ServiceResolver = serviceResolver ?? throw new ArgumentNullException(nameof(serviceResolver));

        _RedisClient = new(() =>
        {
            IRedisClient client;
            if (useConnectionPooling)
            {
                client = new RedisPooledClient(ConvertEndPoints(_ServiceResolver.EndPoints), logger.Error, redisPooledClientOptions);
            }
            else
            {
                client = new RedisClient(ConvertEndPoints(_ServiceResolver.EndPoints), logger.Error, clientOptions);
            }

            _ServiceResolver.PropertyChanged += (sender, args) => RefreshRedisEndPoints(client);

            RefreshRedisEndPoints(client);
            return client;

        }, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    private void RefreshRedisEndPoints(IRedisClient client)
    {
        var newEndPoints = ConvertEndPoints(_ServiceResolver.EndPoints).ToArray();
        client.Refresh(newEndPoints);

        _Logger.Information("Refreshed redis endpoints. New endpoints: {0}", string.Join(", ", newEndPoints));
    }

    private static IEnumerable<string> ConvertEndPoints(IEnumerable<IPEndPoint> endPoints)
        => from e in endPoints
           orderby e.Address.Address, e.Port
           select e.ToString();
}
