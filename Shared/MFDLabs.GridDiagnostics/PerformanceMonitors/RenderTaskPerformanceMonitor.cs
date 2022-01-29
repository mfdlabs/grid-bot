using System;
using MFDLabs.Diagnostics;
using MFDLabs.Instrumentation;

namespace MFDLabs.Grid.Bot.PerformanceMonitors
{
    public sealed class RenderWorkQueuePerformanceMonitor
    {
        private const string Category = "MFDLabs.Grid.WorkQueues.RenderWorkQueue";

        public IRawValueCounter TotalItemsProcessed { get; }
        public IRateOfCountsPerSecondCounter TotalItemsProcessedPerSecond { get; }
        public IRawValueCounter TotalItemsProcessedThatFailed { get; }
        public IRateOfCountsPerSecondCounter TotalItemsProcessedThatFailedPerSecond { get; }
        public IRawValueCounter TotalItemsProcessedThatHadInvalidUserIDs { get; }
        public IRateOfCountsPerSecondCounter TotalItemsProcessedThatHadInvalidUserIDsPerSecond { get; }
        public IRawValueCounter TotalItemsProcessedThatHadNullOrEmptyUsernames { get; }
        public IRateOfCountsPerSecondCounter TotalItemsProcessedThatHadNullOrEmptyUsernamesPerSecond { get; }
        public IRawValueCounter TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccount { get; }
        public IRateOfCountsPerSecondCounter TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccountPerSecond { get; }
        public IRawValueCounter TotalItemsProcessedThatHadBlacklistedUsernames { get; }
        public IRateOfCountsPerSecondCounter TotalItemsProcessedThatHadBlacklistedUsernamesPerSecond { get; }
        public IAverageValueCounter RenderWorkQueueSuccessAverageTimeTicks { get; }
        public IAverageValueCounter RenderWorkQueueFailureAverageTimeTicks { get; }

        public RenderWorkQueuePerformanceMonitor(ICounterRegistry counterRegistry, bool isV2 = false)
        {
            if (counterRegistry == null) throw new ArgumentNullException(nameof(counterRegistry));

            var category = Category;
            if (isV2) category += "V2";

            var instance = $"{SystemGlobal.GetMachineId()} ({SystemGlobal.GetMachineHost()})";

            TotalItemsProcessed = counterRegistry.GetRawValueCounter(category, "TotalItemsProcessed", instance);
            TotalItemsProcessedPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(category, "TotalItemsProcessedPerSecond", instance);
            TotalItemsProcessedThatFailed = counterRegistry.GetRawValueCounter(category, "TotalItemsProcessedThatFailed", instance);
            TotalItemsProcessedThatFailedPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(category, "TotalItemsProcessedThatFailedPerSecond", instance);
            TotalItemsProcessedThatHadInvalidUserIDs = counterRegistry.GetRawValueCounter(category, "TotalItemsProcessedThatHadInvalidUserIDs", instance);
            TotalItemsProcessedThatHadInvalidUserIDsPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(category, "TotalItemsProcessedThatHadInvalidUserIDsPerSecond", instance);
            TotalItemsProcessedThatHadNullOrEmptyUsernames = counterRegistry.GetRawValueCounter(category, "TotalItemsProcessedThatHadNullOrEmptyUsernames", instance);
            TotalItemsProcessedThatHadNullOrEmptyUsernamesPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(category, "TotalItemsProcessedThatHadNullOrEmptyUsernamesPerSecond", instance);
            TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccount = counterRegistry.GetRawValueCounter(
                category,
                "TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccount",
                instance
            );
            TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccountPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(
                category,
                "TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccountPerSecond",
                instance
            );
            TotalItemsProcessedThatHadBlacklistedUsernames = counterRegistry.GetRawValueCounter(category, "TotalItemsProcessedThatHadBlacklistedUsernames", instance);
            TotalItemsProcessedThatHadBlacklistedUsernamesPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(category, "TotalItemsProcessedThatHadBlacklistedUsernamesPerSecond", instance);
            RenderWorkQueueSuccessAverageTimeTicks = counterRegistry.GetAverageValueCounter(category, "RenderWorkQueueSuccessAverageTimeTicks", instance);
            RenderWorkQueueFailureAverageTimeTicks = counterRegistry.GetAverageValueCounter(category, "RenderWorkQueueFailureAverageTimeTicks", instance);
        }
    }

    public sealed class RenderTaskPerformanceMonitor
    {
        private const string Category = "MFDLabs.Grid.Tasks.RenderTask";

        public IRawValueCounter TotalItemsProcessed { get; }

        public IRawValueCounter TotalItemsProcessedThatFailed { get; }

        public IRawValueCounter TotalItemsProcessedThatHadInvalidUserIDs { get; }

        public IRawValueCounter TotalItemsProcessedThatHadNullOrEmptyUsernames { get; }

        public IRawValueCounter TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccount { get; }

        public RenderTaskPerformanceMonitor(ICounterRegistry counterRegistry, bool isV2 = false)
        {
            if (counterRegistry == null) throw new ArgumentNullException(nameof(counterRegistry));

            var category = Category;
            if (isV2) category += "V2";

            var instance = $"{SystemGlobal.GetMachineId()} ({SystemGlobal.GetMachineHost()})";

            TotalItemsProcessed = counterRegistry.GetRawValueCounter(category, "TotalItemsProcessed", instance);
            TotalItemsProcessedThatFailed = counterRegistry.GetRawValueCounter(category, "TotalItemsProcessedThatFailed", instance);
            TotalItemsProcessedThatHadInvalidUserIDs = counterRegistry.GetRawValueCounter(category, "TotalItemsProcessedThatHadInvalidUserIDs", instance);
            TotalItemsProcessedThatHadNullOrEmptyUsernames = counterRegistry.GetRawValueCounter(category, "TotalItemsProcessedThatHadNullOrEmptyUsernames", instance);
            TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccount = counterRegistry.GetRawValueCounter(
                category,
                "TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccount",
                instance);
        }
    }
}
