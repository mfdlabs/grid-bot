using System;
using MFDLabs.Diagnostics;
using MFDLabs.Instrumentation;

namespace MFDLabs.Grid.Bot.PerformanceMonitors
{
    internal sealed class RenderTaskPerformanceMonitor
    {
        private const string _Category = "MFDLabs.Grid.Tasks.RenderTask";

        internal IRawValueCounter TotalItemsProcessed { get; }

        internal IRawValueCounter TotalItemsProcessedThatFailed { get; }

        internal IRawValueCounter TotalItemsProcessedThatHadInvalidUserIDs { get; }

        internal IRawValueCounter TotalItemsProcessedThatHadNullOrEmptyUsernames { get; }

        internal IRawValueCounter TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccount { get; }

        internal RenderTaskPerformanceMonitor(ICounterRegistry counterRegistry)
        {
            if (counterRegistry == null) throw new ArgumentNullException("counterRegistry");

            var instance = $"{SystemGlobal.Singleton.GetMachineID()} ({SystemGlobal.Singleton.GetMachineHost()})";

            TotalItemsProcessed = counterRegistry.GetRawValueCounter(_Category, "TotalItemsProcessed", instance);
            TotalItemsProcessedThatFailed = counterRegistry.GetRawValueCounter(_Category, "TotalItemsProcessedThatFailed", instance);
            TotalItemsProcessedThatHadInvalidUserIDs = counterRegistry.GetRawValueCounter(_Category, "TotalItemsProcessedThatHadInvalidUserIDs", instance);
            TotalItemsProcessedThatHadNullOrEmptyUsernames = counterRegistry.GetRawValueCounter(_Category, "TotalItemsProcessedThatHadNullOrEmptyUsernames", instance);
            TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccount = counterRegistry.GetRawValueCounter(_Category, "TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccount", instance);
        }
    }
}
