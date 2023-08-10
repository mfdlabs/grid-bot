using Redis;
using Logging;
using Configuration;
using Instrumentation;
using ServiceDiscovery;

namespace Grid.Bot.Utility
{
    public static class FloodCheckersRedisClientProvider
    {
#if DEBUG
        private const string _environmentName = "development";
#else
        private const string _environmentName = "production";
#endif

        private const string _floodCheckersRedisPerformanceCategory = "Grid.FloodCheckers.Redis";

        private static IRedisClientProvider _redisClientProvider;

        public static IRedisClient RedisClient => _redisClientProvider.Client;

        public static void SetUp()
        {
            var consulClientProvider = new LocalConsulClientProvider(global::Grid.Bot.Properties.Settings.Default);
            var serviceResolver = new ConsulHttpServiceResolver(
                Logger.Singleton,
                consulClientProvider,
                global::Grid.Bot.Properties.Settings.Default.ToSingleSetting(s => s.FloodCheckersConsulServiceName),
                _environmentName,
                global::Grid.Bot.Properties.Settings.Default.FloodCheckersRedisUseServiceDiscovery
            );

            _redisClientProvider = new HybridRedisClientProvider(
                Logger.Singleton,
                StaticCounterRegistry.Instance,
                serviceResolver,
                _floodCheckersRedisPerformanceCategory,
                global::Grid.Bot.Properties.Settings.Default.ToSingleSetting(s => s.FloodCheckersRedisUseServiceDiscovery),
                global::Grid.Bot.Properties.Settings.Default.ToSingleSetting(s => s.FloodCheckersRedisEndpoints)
            );
        }
    }
}
