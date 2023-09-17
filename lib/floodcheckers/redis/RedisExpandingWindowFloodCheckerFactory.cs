namespace FloodCheckers.Redis;

using System;

using Logging;
using FloodCheckers.Core;

/// <summary>
/// Implementation of <see cref="IFloodCheckerFactory{TFloodChecker}" /> which will construct IFloodCheckers as <see cref="RedisRollingWindowFloodChecker" />s
/// </summary>
public class RedisExpandingWindowFloodCheckerFactory : IFloodCheckerFactory<RedisExpandingWindowFloodChecker>
{
    private readonly IGlobalFloodCheckerEventLogger _GlobalFloodCheckerEventLogger;

    /// <summary>
    /// Construct a new instance of <see cref="RedisExpandingWindowFloodCheckerFactory"/>
    /// </summary>
    public RedisExpandingWindowFloodCheckerFactory()
    {
        _GlobalFloodCheckerEventLogger = new GlobalFloodCheckerEventLogger();
    }

    /// <inheritdoc cref="IFloodCheckerFactory{TFloodChecker}.GetFloodChecker(string, string, Func{int}, Func{TimeSpan}, Func{bool}, Func{bool}, ILogger)"/>
    public RedisExpandingWindowFloodChecker GetFloodChecker(
        string category,
        string key,
        Func<int> getLimit,
        Func<TimeSpan> getWindowPeriod, 
        Func<bool> isEnabled, 
        Func<bool> recordGlobalFloodedEvents,
        ILogger logger
    )
    {
        return new RedisExpandingWindowFloodChecker(
            category,
            key,
            getLimit,
            getWindowPeriod, 
            isEnabled, 
            recordGlobalFloodedEvents,
            logger, 
            FloodCheckerRedisClient.GetInstance(),
            _GlobalFloodCheckerEventLogger
        );
    }
}
