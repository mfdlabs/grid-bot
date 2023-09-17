using Redis;

namespace FloodCheckers.Redis;

using System;

using Logging;
using FloodCheckers.Core;

/// <summary>
/// Base class for Redis based flood checkers.
/// </summary>
public abstract class BaseRedisFloodChecker
{
    /// <summary>
    /// The Redis client.
    /// </summary>
    protected readonly IRedisClient RedisClient;

    /// <summary>
    /// The category.
    /// </summary>
    protected readonly string Category;

    /// <summary>
    /// The key.
    /// </summary>
    protected readonly string Key;

    /// <summary>
    /// The getter that determines if this flood checker is enabled or not.
    /// </summary>
    protected readonly Func<bool> IsEnabled;

    /// <summary>
    /// The <see cref="ILogger"/>
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// The getter that determines if global flood events should be recorded or not.
    /// </summary>
    protected readonly Func<bool> RecordGlobalFloodedEvents;

    /// <summary>
    /// The <see cref="IGlobalFloodCheckerEventLogger"/>
    /// </summary>
    protected readonly IGlobalFloodCheckerEventLogger GlobalFloodCheckerEventLogger;

    private readonly ISettings _Settings;
    private readonly Func<int> _GetLimit;
    private readonly Func<TimeSpan> _GetWindowPeriod;

    /// <summary>
    /// Construct a new instance of <see cref="BaseRedisFloodChecker"/>
    /// </summary>
    /// <param name="category">The flood checker category.</param>
    /// <param name="key">The flood checker key.</param>
    /// <param name="getLimit">The getter for the limit.</param>
    /// <param name="getWindowPeriod">The getter for the window period.</param>
    /// <param name="isEnabled">Is this flood checker enabled?</param>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="redisClient">The <see cref="IRedisClient"/></param>
    /// <param name="recordGlobalFloodedEvents">The getter that determines if global flood events are recorded or not.</param>
    /// <param name="globalFloodCheckerEventLogger">The <see cref="IGlobalFloodCheckerEventLogger"/></param>
    /// <param name="settings">The <see cref="ISettings"/></param>
    protected BaseRedisFloodChecker(
        string category, 
        string key, Func<int> getLimit,
        Func<TimeSpan> getWindowPeriod, 
        Func<bool> isEnabled,
        ILogger logger,
        IRedisClient redisClient,
        Func<bool> recordGlobalFloodedEvents,
        IGlobalFloodCheckerEventLogger globalFloodCheckerEventLogger,
        ISettings settings
    )
    {
        RedisClient = redisClient;
        RecordGlobalFloodedEvents = recordGlobalFloodedEvents;
        GlobalFloodCheckerEventLogger = globalFloodCheckerEventLogger;
        Category = category;
        Key = key;
        Logger = logger;
        IsEnabled = isEnabled;

        _GetLimit = getLimit;
        _GetWindowPeriod = getWindowPeriod;
        _Settings = settings ?? Settings.Singleton;
    }

    /// <summary>
    /// Check the status of the flood checker.
    /// </summary>
    /// <returns>An <see cref="IFloodCheckerStatus"/></returns>
    public IFloodCheckerStatus Check()
    {
        bool isFlooded = false;
        int limit = 0;
        int count = 0;

        if (!IsEnabled())
            return new FloodCheckerStatus(false, limit, count, Category);

        try
        {
            count = GetCount();
            limit = GetLimit();
            isFlooded = count >= limit;

            if (isFlooded)
                RaiseOnFloodedEvent(Category ?? GetType().FullName);
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }

        return new FloodCheckerStatus(isFlooded, limit, count, Category);
    }

    /// <summary>
    /// Is the flood checker flooded?
    /// </summary>
    /// <returns>True if the <see cref="IFloodCheckerStatus.IsFlooded"/> is true.</returns>
    public bool IsFlooded() => Check().IsFlooded;

    /// <summary>
    /// Update the request count.
    /// </summary>
    public void UpdateCount()
    {
        if (!IsEnabled()) return;

        try
        {
            DoUpdateCount();
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }

    /// <summary>
    /// Reset the request count.
    /// </summary>
    public void Reset()
    {
        if (!IsEnabled()) return;

        try
        {
            DoReset();
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }

    /// <summary>
    /// Get the request count.
    /// </summary>
    /// <returns>The request count.</returns>
    public int GetCount()
    {
        if (!IsEnabled()) return 0;

        try
        {
            return DoGetCount();
        }
        catch (Exception ex)
        {
            Logger.Error(ex);

            return 0;
        }
    }

    /// <summary>
    /// Get the request count over the Limit.
    /// </summary>
    /// <returns>The request count over the limit.</returns>
    public int GetCountOverLimit()
    {
        if (!IsEnabled()) return 0;

        try
        {
            int count = GetCount();
            int limit = GetLimit();

            return count > limit 
                ? count - limit 
                : 0;
        }
        catch (Exception ex)
        {
            Logger.Error(ex);

            return 0;
        }
    }

    /// <summary>
    /// Get the seconds to retry after.
    /// </summary>
    /// <returns></returns>
    public int? RetryAfter()
    {
        var retryAfter = DoGetRetryAfter();

        return (int?)retryAfter?.TotalSeconds;
    }

    /// <summary>
    /// Raise the <see cref="IGlobalFloodCheckerEventLogger.RecordFloodCheckerIsFlooded(string)"/>
    /// </summary>
    /// <param name="category">The category.</param>
    protected virtual void RaiseOnFloodedEvent(string category)
    {
        if (GlobalFloodCheckerEventLogger != null)
            if (RecordGlobalFloodedEvents?.Invoke() == true)
                GlobalFloodCheckerEventLogger.RecordFloodCheckerIsFlooded(category);
    }

    /// <summary>
    /// Get the request limit.
    /// </summary>
    /// <returns>The request limit.</returns>
    protected int GetLimit()
    {
        int limit = _GetLimit();
        var windowPeriod = _GetWindowPeriod();
        var minimumWindowPeriod = GetMinimumWindowPeriod();

        if (windowPeriod < minimumWindowPeriod && windowPeriod.TotalMilliseconds > 0)
        {
            int factor = (int)Math.Ceiling(minimumWindowPeriod.TotalMilliseconds / windowPeriod.TotalMilliseconds);
            limit *= factor;
        }

        return limit;
    }

    /// <summary>
    /// Get the window period.
    /// </summary>
    /// <returns>The window period.</returns>
    protected TimeSpan GetWindowPeriod()
    {
        var windowPeriod = _GetWindowPeriod();
        var minimumWindowPeriod = GetMinimumWindowPeriod();

        if (windowPeriod < minimumWindowPeriod && windowPeriod.TotalMilliseconds > 0)
            windowPeriod = TimeSpan.FromMilliseconds(
                (int)Math.Ceiling(minimumWindowPeriod.TotalMilliseconds / 
                windowPeriod.TotalMilliseconds) * 
                windowPeriod.TotalMilliseconds);

        return windowPeriod;
    }

    private TimeSpan GetMinimumWindowPeriod() => _Settings.FloodCheckerMinimumWindowPeriod;

    /// <summary>
    /// Updates the count.
    /// </summary>
    protected abstract void DoUpdateCount();

    /// <summary>
    /// Resets the count.
    /// </summary>
    protected abstract void DoReset();

    /// <summary>
    /// Gets the count.
    /// </summary>
    /// <returns>The count.</returns>
    protected abstract int DoGetCount();

    /// <summary>
    /// Gets the retry-after.
    /// </summary>
    /// <returns>The retry-after.</returns>
    protected virtual TimeSpan? DoGetRetryAfter() => null;
}
