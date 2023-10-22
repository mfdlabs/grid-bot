namespace FloodCheckers.Core;

using System;

/// <summary>
/// A FloodChecker which makes available the amount of time before the current count will expire, and the count return to zero
/// </summary>
public interface IExpiringFloodChecker : IFloodChecker
{
    /// <summary>
    /// The amount of time before the current count will expire, and the count return to zero. Null if there is no current value
    /// </summary>
    TimeSpan? TimeToExpiry();
}
