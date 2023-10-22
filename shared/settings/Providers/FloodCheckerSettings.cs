namespace Grid.Bot;

using System;

using Redis;

/// <summary>
/// Settings provider for the render and script execution flood checkers.
/// </summary>
public class FloodCheckerSettings : BaseSettingsProvider, IHybridRedisClientProviderSettings
{
    /// <inheritdoc cref="Configuration.IVaultProvider.Path"/>
    public override string Path => SettingsProvidersDefaults.FloodCheckerPath;

    /// <summary>
    /// Gets the flood checkers Redis service name in Consul
    /// </summary>
    public string FloodCheckersConsulServiceName => GetOrDefault(
        nameof(FloodCheckersConsulServiceName),
        "floodcheckers-redis"
    );

    /// <summary>
    /// Should floodcheckers Redis use service discovery?
    /// </summary>
    public bool FloodCheckersRedisUseServiceDiscovery => GetOrDefault(
        nameof(FloodCheckersRedisUseServiceDiscovery),
        false
    );

    /// <summary>
    /// Gets the static Redis endpoints to use for flood checkers.
    /// </summary>
    public RedisEndpoints FloodCheckersRedisEndpoints => GetOrDefault(
        nameof(FloodCheckersRedisEndpoints),
        new RedisEndpoints("127.0.0.1:6379")
    );

    /// <summary>
    /// Limit for the script execution flood checker
    /// </summary>
    public int ScriptExecutionFloodCheckerLimit => GetOrDefault(
        nameof(ScriptExecutionFloodCheckerLimit),
        100
    );

    /// <summary>
    /// Gets the window to check for the script execution flood checker.
    /// </summary>
    public TimeSpan ScriptExecutionFloodCheckerWindow => GetOrDefault(
        nameof(ScriptExecutionFloodCheckerWindow),
        TimeSpan.FromHours(1)
    );

    /// <summary>
    /// Is the script execution flood checker enabled?
    /// </summary>
    public bool ScriptExecutionFloodCheckingEnabled => GetOrDefault(
        nameof(ScriptExecutionFloodCheckingEnabled),
        false
    );

    /// <summary>
    /// Limit for the render flood checker
    /// </summary>
    public int RenderFloodCheckerLimit => GetOrDefault(
        nameof(RenderFloodCheckerLimit),
        1000
    );

    /// <summary>
    /// Gets the window to check for the render flood checker.
    /// </summary>
    public TimeSpan RenderFloodCheckerWindow => GetOrDefault(
        nameof(RenderFloodCheckerWindow),
        TimeSpan.FromHours(1)
    );

    /// <summary>
    /// Is the render flood checker enabled?
    /// </summary>
    public bool RenderFloodCheckingEnabled => GetOrDefault(
        nameof(RenderFloodCheckingEnabled),
        false
    );

    /// <summary>
    /// Limit for the per user script execution flood checker
    /// </summary>
    public int ScriptExecutionPerUserFloodCheckerLimit => GetOrDefault(
        nameof(ScriptExecutionPerUserFloodCheckerLimit),
        10
    );

    /// <summary>
    /// Gets the window to check for the per user script execution flood checker.
    /// </summary>
    public TimeSpan ScriptExecutionPerUserFloodCheckerWindow => GetOrDefault(
        nameof(ScriptExecutionPerUserFloodCheckerWindow),
        TimeSpan.FromMinutes(1)
    );

    /// <summary>
    /// Is the per user script execution flood checker enabled?
    /// </summary>
    public bool ScriptExecutionPerUserFloodCheckingEnabled => GetOrDefault(
        nameof(ScriptExecutionPerUserFloodCheckingEnabled),
        false
    );

    /// <summary>
    /// Limit for the per user render flood checker
    /// </summary>
    public int RenderPerUserFloodCheckerLimit => GetOrDefault(
        nameof(RenderPerUserFloodCheckerLimit),
        50
    );

    /// <summary>
    /// Gets the window to check for the per user render flood checker.
    /// </summary>
    public TimeSpan RenderPerUserFloodCheckerWindow => GetOrDefault(
        nameof(RenderPerUserFloodCheckerWindow),
        TimeSpan.FromMinutes(10)
    );

    /// <summary>
    /// Is the per user render flood checker enabled?
    /// </summary>
    public bool RenderPerUserFloodCheckingEnabled => GetOrDefault(
        nameof(RenderPerUserFloodCheckingEnabled),
        false
    );

    /// <inheritdoc cref="IHybridRedisClientProviderSettings.InitialDiscoveryWaitTime"/>
    public TimeSpan InitialDiscoveryWaitTime => GetOrDefault(
        "FloodCheckers" + nameof(InitialDiscoveryWaitTime),
        TimeSpan.FromSeconds(10)
    );
}
