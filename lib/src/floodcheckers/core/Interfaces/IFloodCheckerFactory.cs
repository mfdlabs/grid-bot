namespace FloodCheckers.Core;

using System;

using Logging;

/// <summary>
/// Factory to construct and return a new floodchecker
/// </summary>
/// <typeparam name="TFloodChecker">The type of <see cref="IBasicFloodChecker"/></typeparam>
public interface IFloodCheckerFactory<out TFloodChecker>
    where TFloodChecker : IBasicFloodChecker
{
    /// <summary>
    /// Gets a <see cref="IFloodChecker"/>
    /// </summary>
    /// <param name="category">The category.</param>
    /// <param name="key">The key.</param>
    /// <param name="getLimit">The getter for the limit.</param>
    /// <param name="getWindowPeriod">The getter for the window period.</param>
    /// <param name="isEnabled">The getter that determines if this flood checker is enabled.</param>
    /// <param name="recordGlobalFloodedEvents">The getter that determines if global flooded events are recorded.</param>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <returns>A <typeparamref name="TFloodChecker"/></returns>
    TFloodChecker GetFloodChecker(
        string category, 
        string key, 
        Func<int> getLimit,
        Func<TimeSpan> getWindowPeriod,
        Func<bool> isEnabled,
        Func<bool> recordGlobalFloodedEvents, 
        ILogger logger
    );
}
