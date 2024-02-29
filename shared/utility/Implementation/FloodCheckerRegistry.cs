namespace Grid.Bot.Utility;

using System;
using System.Collections.Concurrent;

using Logging;

using Redis;
using FloodCheckers.Core;
using FloodCheckers.Redis;

/// <summary>
/// Registry for the floodcheckers.
/// </summary>
public class FloodCheckerRegistry : IFloodCheckerRegistry
{
    private const string _scriptExecutionFloodCheckerCategory = "Grid.ExecuteScript.FloodChecking";
    private const string _renderFloodCheckerCategory = "Grid.Render.FloodChecking";

    private readonly ILogger _logger;
    private readonly IRedisClient _redisClient;
    private readonly FloodCheckerSettings _floodCheckerSettings;

    private readonly ConcurrentDictionary<ulong, IFloodChecker> _perUserScriptExecutionFloodCheckers = new();
    private readonly ConcurrentDictionary<ulong, IFloodChecker> _perUserRenderFloodCheckers = new();

    /// <summary>
    /// Construct a new instance of <see cref="FloodCheckerRegistry"/>.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="redisClient">The <see cref="IRedisClient"/>.</param>
    /// <param name="floodCheckerSettings">The <see cref="FloodCheckerSettings"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="logger"/> cannot be null.
    /// - <paramref name="redisClient"/> cannot be null.
    /// - <paramref name="floodCheckerSettings"/> cannot be null.
    /// </exception>
    public FloodCheckerRegistry(
        ILogger logger,
        IRedisClient redisClient,
        FloodCheckerSettings floodCheckerSettings
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _redisClient = redisClient ?? throw new ArgumentNullException(nameof(redisClient));
        _floodCheckerSettings = floodCheckerSettings ?? throw new ArgumentNullException(nameof(floodCheckerSettings));

        ScriptExecutionFloodChecker = new RedisRollingWindowFloodChecker(
            _scriptExecutionFloodCheckerCategory,
            "ExecuteScript",
            () => _floodCheckerSettings.ScriptExecutionFloodCheckerLimit,
            () => _floodCheckerSettings.ScriptExecutionFloodCheckerWindow,
            () => _floodCheckerSettings.ScriptExecutionFloodCheckingEnabled,
            _logger,
            _redisClient
        );

        RenderFloodChecker = new RedisRollingWindowFloodChecker(
            _renderFloodCheckerCategory,
            "Render",
            () => _floodCheckerSettings.RenderFloodCheckerLimit,
            () => _floodCheckerSettings.RenderFloodCheckerWindow,
            () => _floodCheckerSettings.RenderFloodCheckingEnabled,
            _logger,
            _redisClient
        );
    }

    /// <inheritdoc cref="IFloodCheckerRegistry.ScriptExecutionFloodChecker"/>
    public IFloodChecker ScriptExecutionFloodChecker { get; }

    /// <inheritdoc cref="IFloodCheckerRegistry.RenderFloodChecker"/>
    public IFloodChecker RenderFloodChecker { get; }

    /// <inheritdoc cref="IFloodCheckerRegistry.GetPerUserScriptExecutionFloodChecker(ulong)"/>
    public IFloodChecker GetPerUserScriptExecutionFloodChecker(ulong userId)
        => _perUserScriptExecutionFloodCheckers.GetOrAdd(userId, CreatePerUserScriptExecutionFloodChecker);

    /// <inheritdoc cref="IFloodCheckerRegistry.GetPerUserRenderFloodChecker(ulong)"/>
    public IFloodChecker GetPerUserRenderFloodChecker(ulong userId)
        => _perUserRenderFloodCheckers.GetOrAdd(userId, CreatePerUserRenderFloodChecker);

    private IFloodChecker CreatePerUserScriptExecutionFloodChecker(ulong userId)
    {
        return new RedisRollingWindowFloodChecker(
            _scriptExecutionFloodCheckerCategory,
            $"ExecuteScript:{userId}",
            () => _floodCheckerSettings.ScriptExecutionPerUserFloodCheckerLimit,
            () => _floodCheckerSettings.ScriptExecutionPerUserFloodCheckerWindow,
            () => _floodCheckerSettings.ScriptExecutionPerUserFloodCheckingEnabled,
            _logger,
            _redisClient
        );
    }

    private IFloodChecker CreatePerUserRenderFloodChecker(ulong userId)
    {
        return new RedisRollingWindowFloodChecker(
            _renderFloodCheckerCategory,
            $"Render:{userId}",
            () => _floodCheckerSettings.RenderPerUserFloodCheckerLimit,
            () => _floodCheckerSettings.RenderPerUserFloodCheckerWindow,
            () => _floodCheckerSettings.RenderPerUserFloodCheckingEnabled,
            _logger,
            _redisClient
        );
    }
}
