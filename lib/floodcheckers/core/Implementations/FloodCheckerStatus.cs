namespace FloodCheckers.Core;

/// <summary>
/// Default implementation for <see cref="IFloodCheckerStatus"/>
/// </summary>
/// <seealso cref="IFloodCheckerStatus"/>
public class FloodCheckerStatus : IFloodCheckerStatus
{
    /// <summary>
    /// Construct a new instance of <see cref="FloodCheckerStatus"/>
    /// </summary>
    /// <param name="isFlooded">The <see cref="IsFlooded"/></param>
    /// <param name="limit">The <see cref="Limit"/></param>
    /// <param name="count">The <see cref="Count"/></param>
    /// <param name="floodCheckerName">The <see cref="FloodcheckerName"/></param>
    public FloodCheckerStatus(bool isFlooded, int limit, int count, string floodCheckerName)
    {
        IsFlooded = isFlooded;
        Limit = limit;
        Count = count;
        FloodcheckerName = floodCheckerName;
    }

    /// <inheritdoc cref="IFloodCheckerStatus.IsFlooded"/>
    public bool IsFlooded { get; }

    /// <inheritdoc cref="IFloodCheckerStatus.Limit"/>
    public int Limit { get; }

    /// <inheritdoc cref="IFloodCheckerStatus.Count"/>
    public int Count { get; }

    /// <inheritdoc cref="IFloodCheckerStatus.CountOverLimit"/>
    public int CountOverLimit
    {
        get
        {
            if (Count <= Limit) return 0;

            return Count - Limit;
        }
    }

    /// <inheritdoc cref="IFloodCheckerStatus.FloodcheckerName"/>
    public string FloodcheckerName { get; }
}
