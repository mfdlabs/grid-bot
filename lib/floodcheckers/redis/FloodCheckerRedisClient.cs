using Redis;

namespace FloodCheckers.Redis;

using Configuration;

/// <summary>
/// Constructs the default <see cref="IRedisClient"/>
/// </summary>
public static class FloodCheckerRedisClient
{
    private static IRedisClient _RedisClient;
    private static string _RedisEndpointsCsv = "";

    /// <summary>
    /// Static constructor.
    /// </summary>
    static FloodCheckerRedisClient()
    {
        Settings.Singleton.ReadValueAndMonitorChanges(s => s.FloodCheckerRedisEndpointsCsv,
            endpointsCsv =>
        {
            if (!_RedisEndpointsCsv.Equals(endpointsCsv))
            {
                _RedisEndpointsCsv = endpointsCsv;

                var redisEndpoints = endpointsCsv.Split(',');
                if (_RedisClient == null)
                {
                    _RedisClient = new RedisClient(redisEndpoints);
                    return;
                }

                _RedisClient.Refresh(redisEndpoints);
            }
        });
    }

    /// <summary>
    /// Get the static <see cref="IRedisClient"/>
    /// </summary>
    /// <returns>The <see cref="IRedisClient"/></returns>
    public static IRedisClient GetInstance() => _RedisClient;
}
