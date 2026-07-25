namespace Threading;

using System;

/// <summary>
/// Static methods related to calls.
/// </summary>
public static class Call
{
    /// <summary>
    /// Calls the specified <see cref="Action"/> once.
    /// </summary>
    /// <param name="flag">The once flag.</param>
    /// <param name="action">The action to call once.</param>
    public static void Once(ref OnceFlag flag, Action action)
    {
        if (flag.CheckAndSet()) action();
    }
}
