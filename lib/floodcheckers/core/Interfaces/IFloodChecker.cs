namespace FloodCheckers.Core;

/// <summary>
/// Basic flood checker.
/// </summary>
public interface IFloodChecker : IBasicFloodChecker
{
    /// <summary>
    /// Returns the current state of the FloodChecker
    /// </summary>
    /// <returns>A <see cref="FloodCheckerStatus"/> representing the current state of the FloodChecker</returns>
    IFloodCheckerStatus Check();

    /// <summary>
    /// Gets the current count of the FloodChecker
    /// </summary>
    /// <returns>The current count of the FloodChecker</returns>
    int GetCount();

    /// <summary>
    /// Gets the magnitude that count currently exceeds the limit.
    /// If the count is currently at or below the limit this will return zero.
    /// </summary>
    /// <returns>The magnitude that count currently exceeds the limit</returns>
    int GetCountOverLimit();
}
