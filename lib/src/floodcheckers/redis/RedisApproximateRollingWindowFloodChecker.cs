﻿using Redis;

namespace FloodCheckers.Redis;

using System;

using StackExchange.Redis;

using Logging;
using FloodCheckers.Core;


/// <summary>
/// This flood checker is similar to <see cref="RedisRollingWindowFloodChecker" />. It uses a linear approximation of the
/// rate of events and therefore is not as precise as <see cref="RedisRollingWindowFloodChecker" />. On the other hand,
/// it uses very little memory (two numbers), which makes it suitable for rate limiting at high rates.
/// Inspired by https://blog.cloudflare.com/counting-things-a-lot-of-different-things/.
/// </summary>
public class RedisApproximateRollingWindowFloodChecker : BaseRedisFloodChecker, IRetryAfterFloodChecker
{
    private readonly Func<DateTime> _NowProvider;
    private string _LastBucketUpdated;

    /// <summary>
    /// Constructs a new Redis-backed Floodchecker
    /// </summary>
    /// <param name="category">A category for the floodchecker. This will be used for plotting floodchecker metrics and should be broad</param>
    /// <param name="key">The key for the individual action you wish to flood check, which may be much more specific than the category</param>
    /// <param name="getLimit">The threshold before a checker becomes flooded</param>
    /// <param name="getWindowPeriod">The window of time to consider counts towards the limit</param>
    /// <param name="isEnabled">Whether or not the floodchecker is enabled. If false it will never report itself as flooded</param>
    /// <param name="logger">The <see cref="ILogger"/></param>
    public RedisApproximateRollingWindowFloodChecker(
        string category, 
        string key, 
        Func<int> getLimit,
        Func<TimeSpan> getWindowPeriod, 
        Func<bool> isEnabled,
        ILogger logger
    ) 
        : this(
              category, 
              key,
              getLimit,
              getWindowPeriod,
              isEnabled, 
              logger, 
              () => false,
              null
            )
    {
    }

    /// <summary>
    /// Constructs a new Redis-backed Floodchecker
    /// </summary>
    /// <param name="category">A category for the floodchecker. This will be used for plotting floodchecker metrics and should be broad</param>
    /// <param name="key">The key for the individual action you wish to flood check, which may be much more specific than the category</param>
    /// <param name="getLimit">The threshold before a checker becomes flooded</param>
    /// <param name="getWindowPeriod">The window of time to consider counts towards the limit</param>
    /// <param name="isEnabled">Whether or not the floodchecker is enabled. If false it will never report itself as flooded</param>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="redisClient">The <see cref="IRedisClient"/></param>
    public RedisApproximateRollingWindowFloodChecker(
        string category,
        string key,
        Func<int> getLimit,
        Func<TimeSpan> getWindowPeriod,
        Func<bool> isEnabled,
        ILogger logger,
        IRedisClient redisClient
    )
        : this(
              category,
              key,
              getLimit,
              getWindowPeriod,
              isEnabled,
              logger,
              () => false,
              redisClient,
              null
            )
    {
    }

    internal RedisApproximateRollingWindowFloodChecker(
        string category, 
        string key,
        Func<int> getLimit,
        Func<TimeSpan> getWindowPeriod,
        Func<bool> isEnabled, 
        ILogger logger,
        Func<bool> recordGlobalFloodedEvents, 
        IGlobalFloodCheckerEventLogger globalFloodCheckerEventLogger
    )
        : this(
              category, 
              key, 
              getLimit,
              getWindowPeriod,
              isEnabled, 
              logger, 
              recordGlobalFloodedEvents, 
              globalFloodCheckerEventLogger,
              FloodCheckerRedisClient.GetInstance(), 
              () => DateTime.UtcNow,
              null
            )
    {
    }

    internal RedisApproximateRollingWindowFloodChecker(
        string category,
        string key,
        Func<int> getLimit,
        Func<TimeSpan> getWindowPeriod,
        Func<bool> isEnabled,
        ILogger logger,
        Func<bool> recordGlobalFloodedEvents,
        IRedisClient redisClient,
        IGlobalFloodCheckerEventLogger globalFloodCheckerEventLogger
    )
        : this(
              category,
              key,
              getLimit,
              getWindowPeriod,
              isEnabled,
              logger,
              recordGlobalFloodedEvents,
              globalFloodCheckerEventLogger,
              redisClient,
              () => DateTime.UtcNow,
              null
            )
    {
    }

    internal RedisApproximateRollingWindowFloodChecker(
        string category,
        string key, 
        Func<int> getLimit, 
        Func<TimeSpan> getWindowPeriod, 
        Func<bool> isEnabled,
        ILogger logger, 
        Func<bool> recordGlobalFloodedEvents,
        IGlobalFloodCheckerEventLogger globalFloodCheckerEventLogger,
        IRedisClient redisClient,
        Func<DateTime> nowProvider,
        ISettings settings = null
    ) 
        : base(
              category, 
              key,
              getLimit,
              getWindowPeriod,
              isEnabled,
              logger, 
              redisClient,
              recordGlobalFloodedEvents,
              globalFloodCheckerEventLogger, 
              settings
            )
    {
        _NowProvider = nowProvider;
    }

    /// <inheritdoc cref="BaseRedisFloodChecker.DoUpdateCount"/>
    protected override void DoUpdateCount()
    {
        var timeNow = _NowProvider();
        var window = GetWindowPeriod();
        var bucketKey = GetBucketKey(timeNow, window);

        RedisClient.Execute(bucketKey, db => db.StringIncrement(bucketKey));

        if (bucketKey != _LastBucketUpdated)
        {
            RedisClient.Execute(bucketKey, db => db.KeyExpire(bucketKey, GetBucketExpiryTimeSpan(window), CommandFlags.FireAndForget));

            _LastBucketUpdated = bucketKey;
        }
    }

    /// <inheritdoc cref="BaseRedisFloodChecker.DoReset"/>
    protected override void DoReset()
    {
        var window = GetWindowPeriod();
        var timeNow = _NowProvider();
        var currentBucketKey = GetBucketKey(timeNow, window);

        RedisClient.Execute(currentBucketKey, db => db.KeyDelete(currentBucketKey));

        var previousBucketKey = GetBucketKey(timeNow - window, window);
        if (previousBucketKey != currentBucketKey)
            RedisClient.Execute(previousBucketKey, db => db.KeyDelete(previousBucketKey));
    }

    /// <inheritdoc cref="BaseRedisFloodChecker.DoGetCount"/>
    protected override int DoGetCount()
    {
        var window = GetWindowPeriod();
        var timeNow = _NowProvider();
        var currentBucketKey = GetBucketKey(timeNow, window);

        int count = (int)RedisClient.Execute(currentBucketKey, db => db.StringGet(currentBucketKey));

        var previousBucketKey = GetBucketKey(timeNow - window, window);
        if (previousBucketKey != currentBucketKey)
        {
            int previousBucketCount = (int)RedisClient.Execute(previousBucketKey, db => db.StringGet(previousBucketKey));
            var intervalStartTime = timeNow - window;
            long bucketStartTime = GetBucketStart(timeNow - window, window);

            count += (int)(previousBucketCount * (1 - (intervalStartTime.Ticks - bucketStartTime) / window.Ticks));
        }

        return count;
    }

    /// <inheritdoc cref="BaseRedisFloodChecker.DoGetRetryAfter"/>
    protected override TimeSpan? DoGetRetryAfter()
    {
        var window = GetWindowPeriod();
        var timeNow = _NowProvider();
        int limit = GetLimit();
        var currentBucketKey = GetBucketKey(timeNow, window);
        int count = (int)RedisClient.Execute(currentBucketKey, db => db.StringGet(currentBucketKey));

        if (count >= limit)
        {
            double factor = 1 - limit / count;
            long retryAfter = GetBucketStart(timeNow, window) + (long)Math.Round(window.Ticks * factor) + window.Ticks;

            return retryAfter >= timeNow.Ticks 
                ? TimeSpan.FromTicks(retryAfter - timeNow.Ticks) 
                : TimeSpan.Zero;
        }

        limit -= count;

        var previousBucketKey = GetBucketKey(timeNow - window, window);
        if (previousBucketKey != currentBucketKey)
        {
            count = (int)RedisClient.Execute(previousBucketKey, db => db.StringGet(previousBucketKey));
            if (count >= limit)
            {
                double factor = 1 - (double)limit / (double)count;
                long retryAfter = GetBucketStart(timeNow - window, window) + (long)Math.Round(window.Ticks * factor) + window.Ticks;

                return retryAfter >= timeNow.Ticks
                    ? TimeSpan.FromTicks(retryAfter - timeNow.Ticks)
                    : TimeSpan.Zero;
            }
        }

        return TimeSpan.Zero;
    }

    private long GetBucketStart(DateTime time, TimeSpan window) => time.Ticks - time.Ticks % window.Ticks;
    private string GetBucketKey(DateTime time, TimeSpan window) => string.Format("FloodChecker_{0}_{1}", Key, GetBucketStart(time, window));
    private TimeSpan GetBucketExpiryTimeSpan(TimeSpan window) => new(window.Ticks * 2);
}
