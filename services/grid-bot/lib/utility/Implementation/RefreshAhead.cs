namespace Grid.Bot.Utility;

using System;
using System.Threading;


/// <summary>
/// Refresh ahead cached value.
/// </summary>
public class RefreshAhead<T> : IDisposable
{
    private DateTime _LastRefresh = DateTime.MinValue;
    private bool _RunningRefresh;
    private TimeSpan _RefreshInterval;

    private readonly Timer _RefreshTimer;
    private readonly Func<T, T> _RefreshDelegate;

    /// <summary>
    /// Gets the interval since refresh.
    /// </summary>
    public TimeSpan IntervalSinceRefresh => DateTime.Now.Subtract(_LastRefresh);

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
        _RefreshInterval = refreshInterval;
        _RefreshDelegate = refreshDelegate ?? throw new ArgumentNullException(nameof(refreshDelegate));

        Value = initialValue;
        _LastRefresh = DateTime.UtcNow;

        var refreshIntervalInMilliseconds = (int)refreshInterval.TotalMilliseconds;
        _RefreshTimer = new Timer(_ => Refresh(), null, refreshIntervalInMilliseconds, refreshIntervalInMilliseconds);
    }

    /// <summary>
    /// Sets a new refresh interval.
    /// </summary>
    /// <param name="newRefreshInterval">The new refresh interval.</param>
    public void SetRefreshInterval(TimeSpan newRefreshInterval)
    {
        var timeSinceLastRefresh = DateTime.UtcNow - _LastRefresh;
        var nextRunTime = newRefreshInterval - timeSinceLastRefresh;
        if (nextRunTime.TotalMilliseconds < 0.0) nextRunTime = TimeSpan.Zero;

        _RefreshTimer.Change(nextRunTime, newRefreshInterval);
        _RefreshInterval = newRefreshInterval;
    }

    /// <summary>
    /// Manually refreshes the current data.
    /// </summary>
    public void Refresh()
    {
        if (_RunningRefresh) return;

        _RunningRefresh = true;

        bool refreshTimer = true;
        try
        {
            Value = _RefreshDelegate.Invoke(Value);

            _LastRefresh = DateTime.UtcNow;
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
            _RunningRefresh = false;

            if (refreshTimer)
                _RefreshTimer.Change(_RefreshInterval, _RefreshInterval);
        }
    }

    /// <summary>
    /// Constructs the and populates a new instance of <see cref="RefreshAhead{T}"/>.
    /// </summary>
    /// <param name="refreshInterval">The refresh interval.</param>
    /// <param name="refreshDelegate">The refresh delegate.</param>
    /// <returns>A new instance of <see cref="RefreshAhead{T}"/>.</returns>
    public static RefreshAhead<T> ConstructAndPopulate(TimeSpan refreshInterval, Func<T> refreshDelegate) 
        => ConstructAndPopulate(refreshInterval, _ => refreshDelegate());

    /// <summary>
    /// Constructs the and populates a new instance of <see cref="RefreshAhead{T}"/>.
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

        _RefreshTimer?.Dispose();
    }

    #endregion IDisposable Members
}