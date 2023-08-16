using System.Collections.Concurrent;

using Logging;
using FloodCheckers.Core;
using FloodCheckers.Redis;

namespace Grid.Bot.Utility
{
    public static class FloodCheckerRegistry
    {
        private const string _scriptExecutionFloodCheckerCategory = "Grid.ExecuteScript.FloodChecking";
        private const string _renderFloodCheckerCategory = "Grid.Render.FloodChecking";

        private static readonly ConcurrentDictionary<ulong, IFloodChecker> _perUserScriptExecutionFloodCheckers = new();
        private static readonly ConcurrentDictionary<ulong, IFloodChecker> _perUserRenderFloodCheckers = new();

        public static readonly IFloodChecker ScriptExecutionFloodChecker = new RedisRollingWindowFloodChecker(
            _scriptExecutionFloodCheckerCategory,
            "ExecuteScript",
            () => global::Grid.Bot.Properties.Settings.Default.ScriptExecutionFloodCheckerLimit,
            () => global::Grid.Bot.Properties.Settings.Default.ScriptExecutionFloodCheckerWindow,
            () => global::Grid.Bot.Properties.Settings.Default.ScriptExecutionFloodCheckingEnabled,
            Logger.Singleton,
            FloodCheckersRedisClientProvider.RedisClient
        );

        public static readonly IFloodChecker RenderFloodChecker = new RedisRollingWindowFloodChecker(
            _scriptExecutionFloodCheckerCategory,
            "Render",
            () => global::Grid.Bot.Properties.Settings.Default.RenderFloodCheckerLimit,
            () => global::Grid.Bot.Properties.Settings.Default.RenderFloodCheckerWindow,
            () => global::Grid.Bot.Properties.Settings.Default.RenderFloodCheckingEnabled,
            Logger.Singleton,
            FloodCheckersRedisClientProvider.RedisClient
        );

        public static IFloodChecker GetPerUserScriptExecutionFloodChecker(ulong userId)
            => _perUserScriptExecutionFloodCheckers.GetOrAdd(userId, CreatePerUserScriptExecutionFloodChecker);

        public static IFloodChecker GetPerUserRenderFloodChecker(ulong userId)
            => _perUserScriptExecutionFloodCheckers.GetOrAdd(userId, CreatePerUserRenderFloodChecker);

        private static IFloodChecker CreatePerUserScriptExecutionFloodChecker(ulong userId)
        {
            return new RedisRollingWindowFloodChecker(
                _scriptExecutionFloodCheckerCategory,
                $"ExecuteScript:{userId}",
                () => global::Grid.Bot.Properties.Settings.Default.ScriptExecutionPerUserFloodCheckerLimit,
                () => global::Grid.Bot.Properties.Settings.Default.ScriptExecutionPerUserFloodCheckerWindow,
                () => global::Grid.Bot.Properties.Settings.Default.ScriptExecutionPerUserFloodCheckingEnabled,
                Logger.Singleton,
                FloodCheckersRedisClientProvider.RedisClient
            );
        }

        private static IFloodChecker CreatePerUserRenderFloodChecker(ulong userId)
        {
            return new RedisRollingWindowFloodChecker(
                _scriptExecutionFloodCheckerCategory,
                $"Render:{userId}",
                () => global::Grid.Bot.Properties.Settings.Default.RenderPerUserFloodCheckerLimit,
                () => global::Grid.Bot.Properties.Settings.Default.RenderPerUserFloodCheckerWindow,
                () => global::Grid.Bot.Properties.Settings.Default.RenderPerUserFloodCheckingEnabled,
                Logger.Singleton,
                FloodCheckersRedisClientProvider.RedisClient
            );
        }
    }
}
