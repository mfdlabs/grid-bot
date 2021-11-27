using System;
using Discord;
using MFDLabs.Instrumentation;

namespace MFDLabs.Grid.Bot.PerformanceMonitors
{
    internal sealed class UserTaskPerformanceMonitor
    {
        private const string _Category = "MFDLabs.Grid.Tasks.{0}User";

        internal IRawValueCounter TotalItemsProcessed { get; }

        internal IRawValueCounter TotalItemsProcessedThatFailed { get; }

        internal IRawValueCounter TotalItemsProcessedThatSucceeded { get; }

        internal UserTaskPerformanceMonitor(ICounterRegistry counterRegistry, string kind, IUser user)
        {
            if (counterRegistry == null) throw new ArgumentNullException("counterRegistry");

            var instance = $"{user.Username}@{user.Id}";

            var category = string.Format(_Category, kind);

            TotalItemsProcessed = counterRegistry.GetRawValueCounter(category, "TotalItemsProcessed", instance);
            TotalItemsProcessedThatFailed = counterRegistry.GetRawValueCounter(category, "TotalItemsProcessedThatFailed", instance);
            TotalItemsProcessedThatSucceeded = counterRegistry.GetRawValueCounter(category, "TotalItemsProcessedThatSucceeded", instance);
        }
    }
}
