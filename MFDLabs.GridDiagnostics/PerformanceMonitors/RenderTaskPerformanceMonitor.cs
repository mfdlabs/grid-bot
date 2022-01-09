using System;
using MFDLabs.Diagnostics;
using MFDLabs.Instrumentation;

namespace MFDLabs.Grid.Bot.PerformanceMonitors
{
    public sealed class RenderTaskPerformanceMonitor
    {
        private const string Category = "MFDLabs.Grid.Tasks.RenderTask";

        public IRawValueCounter TotalItemsProcessed { get; }

        public IRawValueCounter TotalItemsProcessedThatFailed { get; }

        public IRawValueCounter TotalItemsProcessedThatHadInvalidUserIDs { get; }

        public IRawValueCounter TotalItemsProcessedThatHadNullOrEmptyUsernames { get; }

        public IRawValueCounter TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccount { get; }

        public RenderTaskPerformanceMonitor(ICounterRegistry counterRegistry)
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
