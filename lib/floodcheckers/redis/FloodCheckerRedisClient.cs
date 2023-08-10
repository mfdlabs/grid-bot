using Redis;

namespace FloodCheckers.Redis;

using Configuration;
using Instrumentation;

public static class FloodCheckerRedisClient
{
    private static IRedisClient _RedisClient;
    private static string _RedisEndpointsCsv = "";

    static FloodCheckerRedisClient()
    {
        global::FloodCheckers.Redis.Properties.Settings.Default.ReadValueAndMonitorChanges(s => s.FloodCheckerRedisEndpointsCsv,
            endpointsCsv =>
        {
            if (!_RedisEndpointsCsv.Equals(endpointsCsv))
            {
                _RedisEndpointsCsv = endpointsCsv;

                var redisEndpoints = endpointsCsv.Split(',');
                if (_RedisClient == null)
                {
                    _RedisClient = new RedisClient(StaticCounterRegistry.Instance, redisEndpoints, "FloodCheckers.Redis");
                    return;
                }

                _RedisClient.Refresh(redisEndpoints);
            }
        });
    }

    public static IRedisClient GetInstance() => _RedisClient;
}
