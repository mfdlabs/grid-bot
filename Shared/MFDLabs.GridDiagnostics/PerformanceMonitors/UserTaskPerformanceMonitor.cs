using System;
using Discord;
using MFDLabs.Instrumentation;

namespace MFDLabs.Grid.Bot.PerformanceMonitors
{
    public sealed class UserWorkQueuePerformanceMonitor
    {
        private const string Category = "MFDLabs.Grid.WorkQueues.{0}User";

        public IRawValueCounter TotalItemsProcessed { get; }
        public IRateOfCountsPerSecondCounter TotalItemsProcessedPerSecond { get; }
        public IRawValueCounter TotalItemsProcessedThatFailed { get; }
        public IRateOfCountsPerSecondCounter TotalItemsProcessedThatFailedPerSecond { get; }
        public IRawValueCounter TotalItemsProcessedThatSucceeded { get; }
        public IRateOfCountsPerSecondCounter TotalItemsProcessedThatSucceededPerSecond { get; }

        public UserWorkQueuePerformanceMonitor(ICounterRegistry counterRegistry, string kind, IUser user)
        {
            if (counterRegistry == null) throw new ArgumentNullException(nameof(counterRegistry));

            var instance = $"{user.Username}@{user.Id}";

            var category = string.Format(Category, kind);

            TotalItemsProcessed = counterRegistry.GetRawValueCounter(category, "TotalItemsProcessed", instance);
            TotalItemsProcessedPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(category, "TotalItemsProcessedPerSecond", instance);
            TotalItemsProcessedThatFailed = counterRegistry.GetRawValueCounter(category, "TotalItemsProcessedThatFailed", instance);
            TotalItemsProcessedThatFailedPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(category, "TotalItemsProcessedThatFailedPerSecond", instance);
            TotalItemsProcessedThatSucceeded = counterRegistry.GetRawValueCounter(category, "TotalItemsProcessedThatSucceeded", instance);
            TotalItemsProcessedThatSucceededPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(category, "TotalItemsProcessedThatSucceededPerSecond", instance);
        }
    }
}
