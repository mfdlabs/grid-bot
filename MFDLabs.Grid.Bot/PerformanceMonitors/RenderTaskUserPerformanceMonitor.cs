using System;
using Discord;
using MFDLabs.Instrumentation;

namespace MFDLabs.Grid.Bot.PerformanceMonitors
{
    internal sealed class RenderTaskUserPerformanceMonitor
    {
        private const string _Category = "MFDLabs.Grid.Tasks.RenderTaskUser";

        internal IRawValueCounter TotalRenders { get; }

        internal IRawValueCounter TotalRendersThatFailed { get; }

        internal IRawValueCounter TotalRendersThatSucceeded { get; }

        internal RenderTaskUserPerformanceMonitor(ICounterRegistry counterRegistry, IUser user)
        {
            if (counterRegistry == null) throw new ArgumentNullException("counterRegistry");

            var instance = $"{user.Username}@{user.Id}";

            TotalRenders = counterRegistry.GetRawValueCounter(_Category, "TotalItemsProcessed", instance);
            TotalRendersThatFailed = counterRegistry.GetRawValueCounter(_Category, "TotalItemsProcessedThatFailed", instance);
            TotalRendersThatSucceeded = counterRegistry.GetRawValueCounter(_Category, "TotalRendersThatSucceeded", instance);
        }
    }
}
