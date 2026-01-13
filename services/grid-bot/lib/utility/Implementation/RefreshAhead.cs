namespace Grid.Bot.Utility;

using System;
using System.Threading;


/// <summary>
/// Refresh ahead cached value.
/// </summary>
public class RefreshAhead<T> : IDisposable
{
    private DateTime _lastRefresh;
    private bool _runningRefresh;
    private TimeSpan _refreshInterval;

    private readonly Timer _refreshTimer;
    private readonly Func<T, T> _refreshDelegate;

    /// <summary>
    /// Gets the interval since refresh.
    /// </summary>
    public TimeSpan IntervalSinceRefresh => DateTime.Now.Subtract(_lastRefresh);

    /// <summary>
    /// Gets the currently cached value.
    /// </summary>
    public T Value { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshAhead{T}"/> class.
    /// </summary>
    /// <param name="refreshInterval">The refresh interval.</param>
    /// <param name="refreshDelegate">The refresh delegate.</param>
    public RefreshAhead(TimeSpan refreshInterval, Func<T> refreshDelegate)
        : this(default(T), refreshInterval, _ => refreshDelegate())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshAhead{T}"/> class.
    /// </summary>
    /// <param name="initialValue">The initial value.</param>
    /// <param name="refreshInterval">The refresh interval.</param>
    /// <param name="refreshDelegate">The refresh delegate.</param>
    public RefreshAhead(T initialValue, TimeSpan refreshInterval, Func<T> refreshDelegate)
        : this(initialValue, refreshInterval, _ => refreshDelegate())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshAhead{T}"/> class.
    /// </summary>
    /// <param name="initialValue">The initial value.</param>
    /// <param name="refreshInterval">The refresh interval.</param>
    /// <param name="refreshDelegate">The refresh delegate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="refreshDelegate"/> is <see langword="null"/>.</exception>
    public RefreshAhead(T initialValue, TimeSpan refreshInterval, Func<T, T> refreshDelegate)
    {
        _refreshInterval = refreshInterval;
        _refreshDelegate = refreshDelegate ?? throw new ArgumentNullException(nameof(refreshDelegate));

        Value = initialValue;
        _lastRefresh = DateTime.UtcNow;

        var refreshIntervalInMilliseconds = (int)refreshInterval.TotalMilliseconds;
        _refreshTimer = new Timer(_ => Refresh(), null, refreshIntervalInMilliseconds, refreshIntervalInMilliseconds);
    }

    /// <summary>
    /// Sets a new refresh interval.
    /// </summary>
    /// <param name="newRefreshInterval">The new refresh interval.</param>
    public void SetRefreshInterval(TimeSpan newRefreshInterval)
    {
        var timeSinceLastRefresh = DateTime.UtcNow - _lastRefresh;
        var nextRunTime = newRefreshInterval - timeSinceLastRefresh;
        if (nextRunTime.TotalMilliseconds < 0.0) nextRunTime = TimeSpan.Zero;

        _refreshTimer.Change(nextRunTime, newRefreshInterval);
        _refreshInterval = newRefreshInterval;
    }

    /// <summary>
    /// Manually refreshes the current data.
    /// </summary>
    public void Refresh()
    {
        if (_runningRefresh) return;

        _runningRefresh = true;

        var refreshTimer = true;
        try
        {
            Value = _refreshDelegate.Invoke(Value);

            _lastRefresh = DateTime.UtcNow;
        }
        catch (ThreadAbortException)
        {
            refreshTimer = false;

            throw;
        }
        catch (Exception ex)
        {
            Logging.Logger.Singleton.Error(ex);
        }
        finally
        {
            _runningRefresh = false;

            if (refreshTimer)
                _refreshTimer.Change(_refreshInterval, _refreshInterval);
        }
    }

    /// <summary>
    /// Constructs and populates a new instance of <see cref="RefreshAhead{T}"/>.
    /// </summary>
    /// <param name="refreshInterval">The refresh interval.</param>
    /// <param name="refreshDelegate">The refresh delegate.</param>
    /// <returns>A new instance of <see cref="RefreshAhead{T}"/>.</returns>
    public static RefreshAhead<T> ConstructAndPopulate(TimeSpan refreshInterval, Func<T> refreshDelegate) 
        => ConstructAndPopulate(refreshInterval, _ => refreshDelegate());

    /// <summary>
    /// Constructs and populates a new instance of <see cref="RefreshAhead{T}"/>.
    /// </summary>
    /// <param name="refreshInterval">The refresh interval.</param>
    /// <param name="refreshDelegate">The refresh delegate.</param>
    /// <returns>A new instance of <see cref="RefreshAhead{T}"/>.</returns>
    public static RefreshAhead<T> ConstructAndPopulate(TimeSpan refreshInterval, Func<T, T> refreshDelegate) 
        => new(refreshDelegate(default(T)), refreshInterval, refreshDelegate);

    #region IDisposable Members

    /// <inheritdoc cref="IDisposable.Dispose" />
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        _refreshTimer?.Dispose();
    }

    #endregion IDisposable Members
}
