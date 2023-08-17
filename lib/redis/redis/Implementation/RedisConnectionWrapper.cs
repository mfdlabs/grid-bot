namespace Redis;

using StackExchange.Redis;

internal class RedisConnectionWrapper : ConsistentHashConnectionWrapperBase
{
    private readonly IConnectionMultiplexer _ConnectionMultiplexer;

    public override IDatabase Database => _ConnectionMultiplexer.GetDatabase();

    public override ISubscriber Subscriber => _ConnectionMultiplexer.GetSubscriber();

    public override IServer Server { get; }

    public RedisConnectionWrapper(IConnectionMultiplexer connectionMultiplexer, ConfigurationOptions configuration)
        : base(configuration)
    {
        _ConnectionMultiplexer = connectionMultiplexer;

        Server = RedisClientBase<RedisClientOptions>.GetServerFromMultiplexer(_ConnectionMultiplexer);
    }
}
