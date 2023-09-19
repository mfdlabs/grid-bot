namespace Grid.Bot.Utility;

using Redis;
using Logging;
using Configuration;
using Instrumentation;
using ServiceDiscovery;

/// <summary>
/// Provider for the floodchecking Redis client.
/// </summary>
public static class FloodCheckersRedisClientProvider
{
#if DEBUG
    private const string _environmentName = "development";
#else
    private const string _environmentName = "production";
#endif

    private const string _floodCheckersRedisPerformanceCategory = "Grid.FloodCheckers.Redis";

    private static IRedisClient _redisClient;

    /// <summary>
    /// The <see cref="IRedisClient"/>
    /// </summary>
    public static IRedisClient RedisClient => _redisClient;

    /// <summary>
    /// Set up the Redis client.
    /// </summary>
    public static void SetUp()
    {
        var consulClientProvider = new LocalConsulClientProvider(ConsulSettings.Singleton);
        var serviceResolver = new ConsulHttpServiceResolver(
            ConsulSettings.Singleton,
            Logger.Singleton,
            consulClientProvider,
            FloodCheckerSettings.Singleton.ToSingleSetting(s => s.FloodCheckersConsulServiceName),
            _environmentName,
            FloodCheckerSettings.Singleton.FloodCheckersRedisUseServiceDiscovery
        );

        _redisClient = new HybridRedisClientProvider(
            FloodCheckerSettings.Singleton,
            Logger.Singleton,
            StaticCounterRegistry.Instance,
            serviceResolver,
            _floodCheckersRedisPerformanceCategory,
            FloodCheckerSettings.Singleton.ToSingleSetting(s => s.FloodCheckersRedisUseServiceDiscovery),
            FloodCheckerSettings.Singleton.ToSingleSetting(s => s.FloodCheckersRedisEndpoints)
        ).Client;
    }
}
