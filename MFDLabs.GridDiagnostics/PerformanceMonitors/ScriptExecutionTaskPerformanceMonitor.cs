using System;
using MFDLabs.Diagnostics;
using MFDLabs.Instrumentation;

namespace MFDLabs.Grid.Bot.PerformanceMonitors
{
    public sealed class ScriptExecutionTaskPerformanceMonitor
    {
        private const string Category = "MFDLabs.Grid.Tasks.ScriptExecutionTask.PerfmonV2";

        public IRawValueCounter TotalItemsProcessed { get; }

        public IRawValueCounter TotalItemsProcessedThatFailed { get; }

        public IRawValueCounter TotalItemsProcessedThatHadEmptyScripts { get; }

        public IRawValueCounter TotalItemsProcessedThatHadEmptyScriptsButHadAnAttachment { get; }

        public IRawValueCounter TotalItemsProcessedThatHadEmptyScriptsButHadAnNoAttachment { get; }

        public IRawValueCounter TotalItemsProcessedThatHadAnInvalidScriptFile { get; }

        public IRawValueCounter TotalItemsProcessedThatHadBlacklistedKeywords { get; }

        public IRawValueCounter TotalItemsProcessedThatHadUnicode { get; }

        public IRawValueCounter TotalItemsProcessedThatHadAFileResult { get; }

        public ScriptExecutionTaskPerformanceMonitor(ICounterRegistry counterRegistry)
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
