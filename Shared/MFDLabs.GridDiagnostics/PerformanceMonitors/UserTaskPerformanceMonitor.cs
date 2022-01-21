using System;
using Discord;
using MFDLabs.Instrumentation;

namespace MFDLabs.Grid.Bot.PerformanceMonitors
{
    public sealed class UserTaskPerformanceMonitor
    {
        private const string Category = "MFDLabs.Grid.Tasks.{0}User";

        public IRawValueCounter TotalItemsProcessed { get; }

        public IRawValueCounter TotalItemsProcessedThatFailed { get; }

        public IRawValueCounter TotalItemsProcessedThatSucceeded { get; }

        public UserTaskPerformanceMonitor(ICounterRegistry counterRegistry, string kind, IUser user)
        {
            if (counterRegistry == null) throw new ArgumentNullException(nameof(counterRegistry));

            var instance = $"{user.Username}@{user.Id}";

            var category = string.Format(Category, kind);

            TotalItemsProcessed = counterRegistry.GetRawValueCounter(category, "TotalItemsProcessed", instance);
            TotalItemsProcessedThatFailed = counterRegistry.GetRawValueCounter(category, "TotalItemsProcessedThatFailed", instance);
            TotalItemsProcessedThatSucceeded = counterRegistry.GetRawValueCounter(category, "TotalItemsProcessedThatSucceeded", instance);
        }
    }
}
