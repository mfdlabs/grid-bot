﻿namespace Redis;

using System;

using StackExchange.Redis;

internal class RedisConnectionMultiplexerPoolWrapper : ConsistentHashConnectionWrapperBase
{
    private readonly RedisConnectionMultiplexerPool _ConnectionPool;

    public override IDatabase Database => _ConnectionPool.GetConnectionMultiplexer().GetDatabase();
    public override ISubscriber Subscriber => _ConnectionPool.Subscriber;
    public override IServer Server { get; }

    internal RedisConnectionMultiplexerPoolWrapper(RedisConnectionMultiplexerPool connectionPool, ConfigurationOptions configuration) : base(configuration)
    {
        _ConnectionPool = connectionPool ?? throw new ArgumentNullException(nameof(connectionPool));

        Server = RedisClientBase<RedisPooledClientOptions>.GetServerFromMultiplexer(connectionPool.PrimaryConnection);
    }
}