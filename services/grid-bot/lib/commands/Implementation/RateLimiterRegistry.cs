namespace Grid.Bot.Commands;

using System;
using System.Threading;
using System.Collections.Concurrent;

/// <summary>
/// Registry for the rate limiters, limits are re-read from <see cref="FloodCheckerSettings"/> live.
/// </summary>
public class RateLimiterRegistry : IRateLimiterRegistry
{
    private static readonly TimeSpan _sweepInterval = TimeSpan.FromMinutes(1);

    private readonly FloodCheckerSettings _settings;

    private readonly LiveRateLimiter _globalScriptExecution;
    private readonly LiveRateLimiter _globalRender;

    private readonly ConcurrentDictionary<ulong, LiveRateLimiter> _perUserScriptExecution = new();
    private readonly ConcurrentDictionary<ulong, LiveRateLimiter> _perUserRender = new();

    private long _lastSweepTicks = Environment.TickCount64;

    /// <summary>
    /// Construct a new instance of <see cref="RateLimiterRegistry"/>.
    /// </summary>
    /// <param name="settings">The <see cref="FloodCheckerSettings"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> cannot be null.</exception>
    public RateLimiterRegistry(FloodCheckerSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        _globalScriptExecution = new LiveRateLimiter(
            () => _settings.ScriptExecutionFloodCheckingEnabled,
            () => _settings.ScriptExecutionFloodCheckerLimit,
            () => _settings.ScriptExecutionFloodCheckerWindow
        );

        _globalRender = new LiveRateLimiter(
            () => _settings.RenderFloodCheckingEnabled,
            () => _settings.RenderFloodCheckerLimit,
            () => _settings.RenderFloodCheckerWindow
        );
    }

    /// <inheritdoc cref="IRateLimiterRegistry.TryAcquireScriptExecution(ulong)"/>
    public RateLimitScope TryAcquireScriptExecution(ulong userId)
    {
        SweepIfDue();

        if (!_globalScriptExecution.TryAcquire()) return RateLimitScope.Global;

        var perUser = _perUserScriptExecution.GetOrAdd(userId, _ => new LiveRateLimiter(
            () => _settings.ScriptExecutionPerUserFloodCheckingEnabled,
            () => _settings.ScriptExecutionPerUserFloodCheckerLimit,
            () => _settings.ScriptExecutionPerUserFloodCheckerWindow
        ));

        return perUser.TryAcquire() ? RateLimitScope.None : RateLimitScope.PerUser;
    }

    /// <inheritdoc cref="IRateLimiterRegistry.TryAcquireRender(ulong)"/>
    public RateLimitScope TryAcquireRender(ulong userId)
    {
        SweepIfDue();

        if (!_globalRender.TryAcquire()) return RateLimitScope.Global;

        var perUser = _perUserRender.GetOrAdd(userId, _ => new LiveRateLimiter(
            () => _settings.RenderPerUserFloodCheckingEnabled,
            () => _settings.RenderPerUserFloodCheckerLimit,
            () => _settings.RenderPerUserFloodCheckerWindow
        ));

        return perUser.TryAcquire() ? RateLimitScope.None : RateLimitScope.PerUser;
    }

    private void SweepIfDue()
    {
        var last = Interlocked.Read(ref _lastSweepTicks);
        var now = Environment.TickCount64;

        if (now - last < _sweepInterval.TotalMilliseconds) return;
        if (Interlocked.CompareExchange(ref _lastSweepTicks, now, last) != last) return;

        Sweep(_perUserScriptExecution);
        Sweep(_perUserRender);
    }

    // An entry idle for two windows has no remaining permits in use, so evicting it loses no state.
    private static void Sweep(ConcurrentDictionary<ulong, LiveRateLimiter> limiters)
    {
        foreach (var (userId, limiter) in limiters)
        {
            if (limiter.IdleMilliseconds < limiter.CurrentWindow.TotalMilliseconds * 2) continue;

            if (limiters.TryRemove(userId, out var removed))
                removed.DisposeDeferred();
        }
    }
}
