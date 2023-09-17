using Redis;

namespace FloodCheckers.Redis;

using System;
using System.Collections.Generic;

using StackExchange.Redis;

using Logging;
using FloodCheckers.Core;

/// <summary>
/// A Redis-backed continuous rolling-window FloodChecker. To determine if it is flooded it will look at the number
/// of counts recorded in the specified window period, measuring from the current time when the check is performed.
/// Resolution of count times is the storage time as ticks rounded at the precision of a double float (approx.  1/10,000th of a second)
/// </summary>
public class RedisRollingWindowFloodChecker : BaseRedisFloodChecker, IRetryAfterFloodChecker
{
    private readonly Func<DateTime> _NowProvider;

    private const long _BucketSizeWindowFactor = 5;

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
    public RedisRollingWindowFloodChecker(
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
    public RedisRollingWindowFloodChecker(
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

    internal RedisRollingWindowFloodChecker(
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

    internal RedisRollingWindowFloodChecker(
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

    internal RedisRollingWindowFloodChecker(
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

        RedisClient.Execute(bucketKey, db => db.SortedSetAdd(bucketKey, Guid.NewGuid().ToString(), timeNow.Ticks, CommandFlags.FireAndForget));

        if (bucketKey != _LastBucketUpdated)
        {
            RedisClient.Execute(bucketKey, db => db.KeyExpire(bucketKey, GetBucketExpiryTimeSpan(window), CommandFlags.FireAndForget));

            _LastBucketUpdated = bucketKey;
        }
    }

    /// <inheritdoc cref="BaseRedisFloodChecker.DoReset"/>
    protected override void DoReset()
    {
        foreach (var key in GetCurrentBucketKeys(_NowProvider(), GetWindowPeriod()))
            RedisClient.Execute(key, db => db.KeyDelete(key));
    }

    /// <inheritdoc cref="BaseRedisFloodChecker.DoGetCount"/>
    protected override int DoGetCount()
    {
        int count = 0;
        var window = GetWindowPeriod();
        var timeNow = _NowProvider();
        var intervalStartTime = timeNow - window;

        foreach (var key in GetCurrentBucketKeys(timeNow, window))
            count += (int)RedisClient.Execute(key, db => db.SortedSetLength(key, intervalStartTime.Ticks));

        return count;
    }

    /// <inheritdoc cref="BaseRedisFloodChecker.DoGetRetryAfter"/>
    protected override TimeSpan? DoGetRetryAfter()
    {
        var window = GetWindowPeriod();
        var timeNow = _NowProvider();
        var intervalStartTime = timeNow - window;
        int limit = GetLimit();

        foreach (var key in GetCurrentBucketKeys(timeNow, window))
        {
            int count = (int)RedisClient.Execute(key, db => db.SortedSetLength(key, intervalStartTime.Ticks));

            if (count >= limit)
            {
                var samples = RedisClient.Execute(key, db => db.SortedSetRangeByRankWithScores(key, -(long)limit, -(long)limit));
                if (samples.Length == 0) break;

                long retryAfter = (long)samples[0].Score + window.Ticks - timeNow.Ticks;
                return retryAfter < 0L 
                    ? TimeSpan.Zero :
                    TimeSpan.FromTicks((retryAfter + 500) / 1000 * 1000);
            }
            else
                limit -= count;
        }

        return TimeSpan.Zero;
    }

    private IEnumerable<string> GetCurrentBucketKeys(DateTime timeNow, TimeSpan window)
    {
        var endOfWindowBucketKey = GetBucketKey(timeNow, window);
        yield return endOfWindowBucketKey;

        var startOfWindowBucketKey = GetBucketKey(timeNow.Subtract(window), window);
        if (endOfWindowBucketKey != startOfWindowBucketKey)
            yield return startOfWindowBucketKey;
        
        yield break;
    }

    private string GetBucketKey(DateTime time, TimeSpan window)
    {
        var startOfBucket = time.Ticks - time.Ticks % GetBucketTicks(window);

        return string.Format("FloodChecker_{0}_{1}", Key, startOfBucket);
    }

    private static long GetBucketTicks(TimeSpan window) => window.Ticks * _BucketSizeWindowFactor;
    private TimeSpan GetBucketExpiryTimeSpan(TimeSpan window) => new(window.Ticks * 6);
}
