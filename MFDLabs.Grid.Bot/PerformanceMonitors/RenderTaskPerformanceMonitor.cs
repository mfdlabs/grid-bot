using System;
using MFDLabs.Diagnostics;
using MFDLabs.Instrumentation;

namespace MFDLabs.Grid.Bot.PerformanceMonitors
{
    internal sealed class RenderTaskPerformanceMonitor
    {
        private const string Category = "MFDLabs.Grid.Tasks.RenderTask";

        internal IRawValueCounter TotalItemsProcessed { get; }

        internal IRawValueCounter TotalItemsProcessedThatFailed { get; }

        internal IRawValueCounter TotalItemsProcessedThatHadInvalidUserIDs { get; }

        internal IRawValueCounter TotalItemsProcessedThatHadNullOrEmptyUsernames { get; }

        internal IRawValueCounter TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccount { get; }

        internal RenderTaskPerformanceMonitor(ICounterRegistry counterRegistry)
        {
            if (counterRegistry == null) throw new ArgumentNullException(nameof(counterRegistry));

            var instance = $"{SystemGlobal.GetMachineId()} ({SystemGlobal.GetMachineHost()})";

            TotalItemsProcessed = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessed", instance);
            TotalItemsProcessedThatFailed = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessedThatFailed", instance);
            TotalItemsProcessedThatHadInvalidUserIDs = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessedThatHadInvalidUserIDs", instance);
            TotalItemsProcessedThatHadNullOrEmptyUsernames = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessedThatHadNullOrEmptyUsernames", instance);
            TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccount = counterRegistry.GetRawValueCounter(
                Category,
                "TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccount",
                instance);
        }
    }
}
