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

    public RedisRollingWindowFloodCheckerFactory()
    {
        _GlobalFloodCheckerEventLogger = new GlobalFloodCheckerEventLogger();
    }

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
