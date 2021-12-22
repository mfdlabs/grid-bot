using System;
using MFDLabs.Diagnostics;
using MFDLabs.Instrumentation;

namespace MFDLabs.Grid.Bot.PerformanceMonitors
{
    internal sealed class ScriptExecutionTaskPerformanceMonitor
    {
        private const string Category = "MFDLabs.Grid.Tasks.ScriptExecutionTask.PerfmonV2";

        internal IRawValueCounter TotalItemsProcessed { get; }

        internal IRawValueCounter TotalItemsProcessedThatFailed { get; }

        internal IRawValueCounter TotalItemsProcessedThatHadEmptyScripts { get; }

        internal IRawValueCounter TotalItemsProcessedThatHadEmptyScriptsButHadAnAttachment { get; }

        internal IRawValueCounter TotalItemsProcessedThatHadEmptyScriptsButHadAnNoAttachment { get; }

        internal IRawValueCounter TotalItemsProcessedThatHadAnInvalidScriptFile { get; }

        internal IRawValueCounter TotalItemsProcessedThatHadBlacklistedKeywords { get; }

        internal IRawValueCounter TotalItemsProcessedThatHadUnicode { get; }

        internal IRawValueCounter TotalItemsProcessedThatHadAFileResult { get; }

        internal ScriptExecutionTaskPerformanceMonitor(ICounterRegistry counterRegistry)
        {
            if (counterRegistry == null) throw new ArgumentNullException(nameof(counterRegistry));

            var instance = $"{SystemGlobal.GetMachineId()} ({SystemGlobal.GetMachineHost()})";

            TotalItemsProcessed = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessed", instance);
            TotalItemsProcessedThatFailed = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessedThatFailed", instance);
            TotalItemsProcessedThatHadEmptyScripts = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessedThatHadEmptyScripts", instance);
            TotalItemsProcessedThatHadEmptyScriptsButHadAnAttachment = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessedThatHadEmptyScriptsButHadAnAttachment", instance);
            TotalItemsProcessedThatHadEmptyScriptsButHadAnNoAttachment = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessedThatHadEmptyScriptsButHadAnNoAttachment", instance);
            TotalItemsProcessedThatHadAnInvalidScriptFile = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessedThatHadAnInvalidScriptFile", instance);
            TotalItemsProcessedThatHadBlacklistedKeywords = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessedThatHadBlacklistedKeywords", instance);
            TotalItemsProcessedThatHadUnicode = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessedThatHadUnicode", instance);
            TotalItemsProcessedThatHadAFileResult = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessedThatHadAFileResult", instance);
        }
    }
}
