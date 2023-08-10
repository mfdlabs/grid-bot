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

        private static IRedisClient _redisClient;

        public static IRedisClient RedisClient => _redisClient;

        public static void SetUp()
        {
            var consulClientProvider = new LocalConsulClientProvider(global::Grid.Bot.Properties.Settings.Default);
            var serviceResolver = new ConsulHttpServiceResolver(
                global::Grid.Bot.Properties.Settings.Default,
                Logger.Singleton,
                consulClientProvider,
                global::Grid.Bot.Properties.Settings.Default.ToSingleSetting(s => s.FloodCheckersConsulServiceName),
                _environmentName,
                global::Grid.Bot.Properties.Settings.Default.FloodCheckersRedisUseServiceDiscovery
            );

            _redisClient = new HybridRedisClientProvider(
                Logger.Singleton,
                StaticCounterRegistry.Instance,
                serviceResolver,
                _floodCheckersRedisPerformanceCategory,
                global::Grid.Bot.Properties.Settings.Default.ToSingleSetting(s => s.FloodCheckersRedisUseServiceDiscovery),
                global::Grid.Bot.Properties.Settings.Default.ToSingleSetting(s => s.FloodCheckersRedisEndpoints)
            ).Client;
        }
    }
}
