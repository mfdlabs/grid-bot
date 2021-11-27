using System;
using MFDLabs.Diagnostics;
using MFDLabs.Instrumentation;

namespace MFDLabs.Grid.Bot.PerformanceMonitors
{
    internal sealed class ScriptExecutionTaskPerformanceMonitor
    {
        private const string _Category = "MFDLabs.Grid.Tasks.ScriptExecutionTask.PerfmonV2";

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
            if (counterRegistry == null) throw new ArgumentNullException("counterRegistry");

            var instance = $"{SystemGlobal.Singleton.GetMachineID()} ({SystemGlobal.Singleton.GetMachineHost()})";

            TotalItemsProcessed = counterRegistry.GetRawValueCounter(_Category, "TotalItemsProcessed", instance);
            TotalItemsProcessedThatFailed = counterRegistry.GetRawValueCounter(_Category, "TotalItemsProcessedThatFailed", instance);
            TotalItemsProcessedThatHadEmptyScripts = counterRegistry.GetRawValueCounter(_Category, "TotalItemsProcessedThatHadEmptyScripts", instance);
            TotalItemsProcessedThatHadEmptyScriptsButHadAnAttachment = counterRegistry.GetRawValueCounter(_Category, "TotalItemsProcessedThatHadEmptyScriptsButHadAnAttachment", instance);
            TotalItemsProcessedThatHadEmptyScriptsButHadAnNoAttachment = counterRegistry.GetRawValueCounter(_Category, "TotalItemsProcessedThatHadEmptyScriptsButHadAnNoAttachment", instance);
            TotalItemsProcessedThatHadAnInvalidScriptFile = counterRegistry.GetRawValueCounter(_Category, "TotalItemsProcessedThatHadAnInvalidScriptFile", instance);
            TotalItemsProcessedThatHadBlacklistedKeywords = counterRegistry.GetRawValueCounter(_Category, "TotalItemsProcessedThatHadBlacklistedKeywords", instance);
            TotalItemsProcessedThatHadUnicode = counterRegistry.GetRawValueCounter(_Category, "TotalItemsProcessedThatHadUnicode", instance);
            TotalItemsProcessedThatHadAFileResult = counterRegistry.GetRawValueCounter(_Category, "TotalItemsProcessedThatHadAFileResult", instance);
        }
    }
}
