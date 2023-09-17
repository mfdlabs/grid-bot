namespace FloodCheckers.Redis;

using System;

using Logging;
using FloodCheckers.Core;

/// <summary>
/// Implementation of <see cref="IFloodCheckerFactory{TFloodChecker}" /> which will construct IFloodCheckers as <see cref="RedisRollingWindowFloodChecker" />s
/// </summary>
public class RedisRollingWindowFloodCheckerFactory : IFloodCheckerFactory<RedisRollingWindowFloodChecker>
{
    private readonly IGlobalFloodCheckerEventLogger _GlobalFloodCheckerEventLogger;

    /// <summary>
    /// Construct a new instance of <see cref="RedisExpandingWindowFloodCheckerFactory"/>
    /// </summary>
    public RedisRollingWindowFloodCheckerFactory()
    {
        _GlobalFloodCheckerEventLogger = new GlobalFloodCheckerEventLogger();
    }

    /// <inheritdoc cref="IFloodCheckerFactory{TFloodChecker}.GetFloodChecker(string, string, Func{int}, Func{TimeSpan}, Func{bool}, Func{bool}, ILogger)"/>
    public RedisRollingWindowFloodChecker GetFloodChecker(
        string category,
        string key,
        Func<int> getLimit, 
        Func<TimeSpan> getWindowPeriod,
        Func<bool> isEnabled,
        Func<bool> recordGlobalFloodedEvents, 
        ILogger logger
    )
    {
        return new RedisRollingWindowFloodChecker(
            category, 
            key,
            getLimit,
            getWindowPeriod,
            isEnabled, 
            logger,
            recordGlobalFloodedEvents,
            _GlobalFloodCheckerEventLogger
        );
    }
}
