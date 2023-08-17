namespace FloodCheckers.Core;

using System;

public class GlobalFloodCheckerEventLogger : IGlobalFloodCheckerEventLogger
{
    public static event Action<string> OnFlooded;

    internal static void RecordFloodCheckerIsFloodedStatic(string category) => OnFlooded?.Invoke(category);
    public void RecordFloodCheckerIsFlooded(string category) => OnFlooded?.Invoke(category);
}
