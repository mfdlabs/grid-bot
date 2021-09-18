using MFDLabs.Instrumentation;
using MFDLabs.Text.Extensions;
using System;

namespace MFDLabs.Concurrency
{
    /// <summary>
    /// The monitor to be used with <see cref="Base.BaseTask{TSingleton, TItem}"/> and <see cref="Base.Async.AsyncBaseTask{TSingleton, TItem}"/>, along with <see cref="Packet"/>
    /// </summary>
    public sealed class TaskThreadMonitor
    {
        /// <summary>
        /// The count of items processed as a <see cref="IRawValueCounter"/>
        /// </summary>

        public IRawValueCounter CountOfItemsProcessed { get; }

        /// <summary>
        /// The count of items processed as a <see cref="IRateOfCountsPerSecondCounter"/>
        /// </summary>
        public IRateOfCountsPerSecondCounter RateOfItemsPerSecondProcessed { get; }

        /// <summary>
        /// The average items processed as a <see cref="IAverageValueCounter"/>
        /// </summary>
        public IAverageValueCounter AverageRateOfItems { get; }


        /// <summary>
        /// The count of items processed that did succeed as a <see cref="IRawValueCounter"/>
        /// </summary>
        public IRawValueCounter CountOfItemsProcessedThatSucceed { get; }

        /// <summary>
        /// The count of items processed that did succeed as a <see cref="IRateOfCountsPerSecondCounter"/>
        /// </summary>
        public IRateOfCountsPerSecondCounter RateOfItemsPerSecondProcessedThatSucceed { get; }

        /// <summary>
        /// The average items processed that did succeed as a <see cref="IAverageValueCounter"/>
        /// </summary>
        public IAverageValueCounter AverageRateOfItemsThatSucceed { get; }


        /// <summary>
        /// The count of items processed that did fail as a <see cref="IRawValueCounter"/>
        /// </summary>
        public IRawValueCounter CountOfItemsProcessedThatFail { get; }

        /// <summary>
        /// The count of items processed that did fail as a <see cref="IRateOfCountsPerSecondCounter"/>
        /// </summary>
        public IRateOfCountsPerSecondCounter RateOfItemsPerSecondProcessedThatFail { get; }

        /// <summary>
        /// The average items processed that did fail as a <see cref="IAverageValueCounter"/>
        /// </summary>
        public IAverageValueCounter AverageRateOfItemsThatFail { get; }

        internal TaskThreadMonitor(ICounterRegistry registry, string threadName)
        {
            if (registry == null) throw new ArgumentNullException("registry");
            if (threadName.IsNullOrEmpty()) throw new ArgumentNullException(threadName);

            CountOfItemsProcessed = registry.GetRawValueCounter(_Category, "CountOfItemsProcessed", threadName);
            RateOfItemsPerSecondProcessed = registry.GetRateOfCountsPerSecondCounter(_Category, "RateOfItemsPerSecondProcessed", threadName);
            AverageRateOfItems = registry.GetAverageValueCounter(_Category, "AverageRateOfItems", threadName);
            CountOfItemsProcessedThatSucceed = registry.GetRawValueCounter(_Category, "CountOfItemsProcessedThatSucceed", threadName);
            RateOfItemsPerSecondProcessedThatSucceed = registry.GetRateOfCountsPerSecondCounter(_Category, "RateOfItemsPerSecondProcessedThatSucceed", threadName);
            AverageRateOfItemsThatSucceed = registry.GetAverageValueCounter(_Category, "AverageRateOfItemsThatSucceed", threadName);
            CountOfItemsProcessedThatFail = registry.GetRawValueCounter(_Category, "CountOfItemsProcessedThatFail", threadName);
            RateOfItemsPerSecondProcessedThatFail = registry.GetRateOfCountsPerSecondCounter(_Category, "RateOfItemsPerSecondProcessedThatFail", threadName);
            AverageRateOfItemsThatFail = registry.GetAverageValueCounter(_Category, "AverageRateOfItemsThatFail", threadName);
        }

        private const string _Category = "MFDLabs.Concurrency.TaskThread.PerfmonCountersV4";
    }
}
