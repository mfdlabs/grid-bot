namespace FloodCheckers.Core;

/// <summary>
/// Represents a basic flood checker.
/// </summary>
public interface IBasicFloodChecker
{
    /// <summary>
    /// Indicates whether or not the current count has reached or exceeded the limit
    /// </summary>
    /// <returns>True if the count has reached or exceeded the limit.</returns>
    bool IsFlooded();

    /// <summary>
    /// Increases the current count of the FloodChecker by one
    /// </summary>
    void UpdateCount();

    /// <summary>
    /// Resets the count of the FloodChecker to zero
    /// </summary>
    void Reset();
}
