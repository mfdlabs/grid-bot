﻿namespace FloodCheckers.Core;

/// <summary>
/// Event logger for to log when a category of floodcheckers is accessed while the floodchecker is flooded.
/// </summary>
public interface IGlobalFloodCheckerEventLogger
{
    /// <summary>
    /// Record an event when the flood checker is flooded.
    /// </summary>
    /// <param name="category">The category.</param>
    void RecordFloodCheckerIsFlooded(string category);
}
