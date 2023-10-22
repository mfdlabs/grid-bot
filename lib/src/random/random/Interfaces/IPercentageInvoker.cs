namespace Random;

using System;

/// <summary>
/// Base interface for invoking an action based on a percentage.
/// </summary>
public interface IPercentageInvoker
{
    /// <summary>
    /// Can the action be invoked?
    /// </summary>
    /// <param name="invokePercentage">The percentage.</param>
    /// <returns>True if it can be invoked.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="invokePercentage"/> must be between 0 and 100.</exception>
    bool CanInvoke(int invokePercentage);

    /// <summary>
    /// Invoke the specified <see cref="Action"/> constrained.
    /// </summary>
    /// <param name="action">The <see cref="Action"/></param>
    /// <param name="invokePercentage">The percentage.</param>
    /// <returns>True if the action was invoked or not.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="invokePercentage"/> must be between 0 and 100.</exception>
    bool InvokeAction(Action action, int invokePercentage);
}
