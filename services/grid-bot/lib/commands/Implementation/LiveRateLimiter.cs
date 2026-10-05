namespace Grid.Bot.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.RateLimiting;

using Polly;
using Polly.RateLimiting;

/// <summary>
/// A sliding window rate limiter whose limit, window and enabled state are re-read from settings on every acquire.
/// </summary>
internal sealed class LiveRateLimiter(Func<bool> enabled, Func<int> limit, Func<TimeSpan> window) : IDisposable
{
    private static readonly TimeSpan _minimumWindow = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan _disposeDelay = TimeSpan.FromSeconds(5);

    private sealed record State(int Limit, TimeSpan Window, RateLimiter Limiter, ResiliencePipeline Pipeline);

    private readonly Func<bool> _enabled = enabled ?? throw new ArgumentNullException(nameof(enabled));
    private readonly Func<int> _limit = limit ?? throw new ArgumentNullException(nameof(limit));
    private readonly Func<TimeSpan> _window = window ?? throw new ArgumentNullException(nameof(window));
    private readonly object _lock = new();

    private State _state;
    private long _lastUsedTicks = Environment.TickCount64;


    /// <summary>
    /// Gets the current window, used to decide when an idle limiter can be evicted.
    /// </summary>
    public TimeSpan CurrentWindow => _state?.Window ?? _minimumWindow;

    /// <summary>
    /// Gets the milliseconds since this limiter was last used.
    /// </summary>
    public long IdleMilliseconds => Environment.TickCount64 - Interlocked.Read(ref _lastUsedTicks);

    /// <summary>
    /// Try to take a permit.
    /// </summary>
    /// <returns>True if the call is allowed.</returns>
    public bool TryAcquire()
    {
        Interlocked.Exchange(ref _lastUsedTicks, Environment.TickCount64);

        if (!_enabled()) return true;

        try
        {
            GetState().Pipeline.Execute(static () => { });

            return true;
        }
        catch (RateLimiterRejectedException)
        {
            return false;
        }
    }

    public void Dispose() => _state?.Limiter.Dispose();

    /// <summary>
    /// Dispose after a short delay so in-flight acquires on the limiter do not throw.
    /// </summary>
    public void DisposeDeferred()
        => Task.Delay(_disposeDelay).ContinueWith(_ => Dispose(), TaskScheduler.Default);

    private State GetState()
    {
        var limit = Math.Max(1, _limit());
        var window = _window();
        if (window < _minimumWindow) window = _minimumWindow;

        var state = _state;
        if (state != null && state.Limit == limit && state.Window == window) return state;

        State previous;
        lock (_lock)
        {
            state = _state;
            if (state != null && state.Limit == limit && state.Window == window) return state;

            var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = limit,
                Window = window,
                SegmentsPerWindow = 10,
                QueueLimit = 0,
                AutoReplenishment = true,
            });

            var pipeline = new ResiliencePipelineBuilder()
                .AddRateLimiter(limiter)
                .Build();

            previous = _state;
            _state = state = new State(limit, window, limiter, pipeline);
        }

        if (previous != null)
            Task.Delay(_disposeDelay).ContinueWith(_ => previous.Limiter.Dispose(), TaskScheduler.Default);

        return state;
    }
}
