namespace Grid.Bot.Utility;

using System.Collections.Concurrent;

using Logging;

using FloodCheckers.Core;
using FloodCheckers.Redis;

/// <summary>
/// Registry for the flood-checkers.
/// </summary>
public static class FloodCheckerRegistry
{
    private const string _scriptExecutionFloodCheckerCategory = "Grid.ExecuteScript.FloodChecking";
    private const string _renderFloodCheckerCategory = "Grid.Render.FloodChecking";

    private static readonly ConcurrentDictionary<ulong, IFloodChecker> _perUserScriptExecutionFloodCheckers = new();
    private static readonly ConcurrentDictionary<ulong, IFloodChecker> _perUserRenderFloodCheckers = new();

    /// <summary>
    /// Gets the system flood checker for script executions.
    /// </summary>
    public static readonly IFloodChecker ScriptExecutionFloodChecker = new RedisRollingWindowFloodChecker(
        _scriptExecutionFloodCheckerCategory,
        "ExecuteScript",
        () => FloodCheckerSettings.Singleton.ScriptExecutionFloodCheckerLimit,
        () => FloodCheckerSettings.Singleton.ScriptExecutionFloodCheckerWindow,
        () => FloodCheckerSettings.Singleton.ScriptExecutionFloodCheckingEnabled,
        Logger.Singleton,
        FloodCheckersRedisClientProvider.RedisClient
    );

    /// <summary>
    /// Gets the system flood checker for renders.
    /// </summary>
    public static readonly IFloodChecker RenderFloodChecker = new RedisRollingWindowFloodChecker(
        _renderFloodCheckerCategory,
        "Render",
        () => FloodCheckerSettings.Singleton.RenderFloodCheckerLimit,
        () => FloodCheckerSettings.Singleton.RenderFloodCheckerWindow,
        () => FloodCheckerSettings.Singleton.RenderFloodCheckingEnabled,
        Logger.Singleton,
        FloodCheckersRedisClientProvider.RedisClient
    );

    /// <summary>
    /// Get the script execution <see cref="IFloodChecker"/> for the <see cref="Discord.IUser"/>
    /// </summary>
    /// <param name="userId">The ID of the <see cref="Discord.IUser"/></param>
    /// <returns>The script execution <see cref="IFloodChecker"/></returns>
    public static IFloodChecker GetPerUserScriptExecutionFloodChecker(ulong userId)
        => _perUserScriptExecutionFloodCheckers.GetOrAdd(userId, CreatePerUserScriptExecutionFloodChecker);

    /// <summary>
    /// Get the render <see cref="IFloodChecker"/> for the <see cref="Discord.IUser"/>
    /// </summary>
    /// <param name="userId">The ID of the <see cref="Discord.IUser"/></param>
    /// <returns>The render <see cref="IFloodChecker"/></returns>
    public static IFloodChecker GetPerUserRenderFloodChecker(ulong userId)
        => _perUserRenderFloodCheckers.GetOrAdd(userId, CreatePerUserRenderFloodChecker);

    private static IFloodChecker CreatePerUserScriptExecutionFloodChecker(ulong userId)
    {
        return new RedisRollingWindowFloodChecker(
            _scriptExecutionFloodCheckerCategory,
            $"ExecuteScript:{userId}",
            () => FloodCheckerSettings.Singleton.ScriptExecutionPerUserFloodCheckerLimit,
            () => FloodCheckerSettings.Singleton.ScriptExecutionPerUserFloodCheckerWindow,
            () => FloodCheckerSettings.Singleton.ScriptExecutionPerUserFloodCheckingEnabled,
            Logger.Singleton,
            FloodCheckersRedisClientProvider.RedisClient
        );
    }

    private static IFloodChecker CreatePerUserRenderFloodChecker(ulong userId)
    {
        return new RedisRollingWindowFloodChecker(
            _renderFloodCheckerCategory,
            $"Render:{userId}",
            () => FloodCheckerSettings.Singleton.RenderPerUserFloodCheckerLimit,
            () => FloodCheckerSettings.Singleton.RenderPerUserFloodCheckerWindow,
            () => FloodCheckerSettings.Singleton.RenderPerUserFloodCheckingEnabled,
            Logger.Singleton,
            FloodCheckersRedisClientProvider.RedisClient
        );
    }
}
