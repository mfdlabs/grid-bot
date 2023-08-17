using Redis;

namespace FloodCheckers.Redis;

using System;

using Logging;
using FloodCheckers.Core;

using Settings = global::FloodCheckers.Redis.Properties.Settings;

public abstract class BaseRedisFloodChecker
{
    protected readonly IRedisClient RedisClient;
    protected readonly string Category;
    protected readonly string Key;
    protected readonly Func<bool> IsEnabled;
    protected readonly ILogger Logger;
    protected readonly Func<bool> RecordGlobalFloodedEvents;
    protected readonly IGlobalFloodCheckerEventLogger GlobalFloodCheckerEventLogger;

    private readonly ISettings _Settings;
    private readonly Func<int> _GetLimit;
    private readonly Func<TimeSpan> _GetWindowPeriod;

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
        _GetLimit = getLimit;
        _GetWindowPeriod = getWindowPeriod;
        IsEnabled = isEnabled;
        Logger = logger;
        _Settings = (settings ?? Settings.Default);
    }

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

    public bool IsFlooded() => Check().IsFlooded;

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

    public int? RetryAfter()
    {
        var retryAfter = DoGetRetryAfter();

        return (int?)retryAfter?.TotalSeconds;
    }

    protected virtual void RaiseOnFloodedEvent(string category)
    {
        if (GlobalFloodCheckerEventLogger != null)
            if (RecordGlobalFloodedEvents?.Invoke() == true)
                GlobalFloodCheckerEventLogger.RecordFloodCheckerIsFlooded(category);
    }

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

    protected TimeSpan GetWindowPeriod()
    {
        var windowPeriod = _GetWindowPeriod();
        var minimumWindowPeriod = GetMinimumWindowPeriod();

        if (windowPeriod < minimumWindowPeriod && windowPeriod.TotalMilliseconds > 0)
            windowPeriod = TimeSpan.FromMilliseconds((int)Math.Ceiling(minimumWindowPeriod.TotalMilliseconds / windowPeriod.TotalMilliseconds) * windowPeriod.TotalMilliseconds);

        return windowPeriod;
    }

    private TimeSpan GetMinimumWindowPeriod() => _Settings.FloodCheckerMinimumWindowPeriod;

    protected abstract void DoUpdateCount();
    protected abstract void DoReset();
    protected abstract int DoGetCount();
    protected virtual TimeSpan? DoGetRetryAfter() => null;
}
