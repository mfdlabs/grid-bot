namespace FloodCheckers.Redis;

using System;

/// <summary>
/// Settings for Redis flood checkers.
/// </summary>
public interface ISettings
{
    /// <summary>
    /// Gets the minimum window period for the flood checkers.
    /// </summary>
    TimeSpan FloodCheckerMinimumWindowPeriod { get; }
}
