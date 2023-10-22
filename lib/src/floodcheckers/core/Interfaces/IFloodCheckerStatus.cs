namespace FloodCheckers.Core;

/// <summary>
/// The status of a flood checker.
/// </summary>
public interface IFloodCheckerStatus
{
    /// <summary>
    /// Gets the current count.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets the count that is over the <see cref="Limit"/>
    /// </summary>
    int CountOverLimit { get; }

    /// <summary>
    /// Gets the name of the flood checker.
    /// </summary>
    string FloodcheckerName { get; }

    /// <summary>
    /// Is the flood checker flooded?
    /// </summary>
    bool IsFlooded { get; }

    /// <summary>
    /// Gets the limit for the flood checker.
    /// </summary>
    int Limit { get; }
}
