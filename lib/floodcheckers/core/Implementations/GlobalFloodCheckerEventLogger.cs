namespace FloodCheckers.Core;

using System;

/// <inheritdoc cref="IGlobalFloodCheckerEventLogger"/>
public class GlobalFloodCheckerEventLogger : IGlobalFloodCheckerEventLogger
{
    /// <summary>
    /// Gets the event that is invoked when the flood checker is flooded.
    /// </summary>
    public static event Action<string> OnFlooded;

    internal static void RecordFloodCheckerIsFloodedStatic(string category) => OnFlooded?.Invoke(category);

    /// <inheritdoc cref="IGlobalFloodCheckerEventLogger.RecordFloodCheckerIsFlooded(string)"/>
    public void RecordFloodCheckerIsFlooded(string category) => OnFlooded?.Invoke(category);
}
