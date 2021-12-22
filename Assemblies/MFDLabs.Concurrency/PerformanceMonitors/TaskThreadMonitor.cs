using System;
using MFDLabs.Instrumentation;
using MFDLabs.Text.Extensions;

// ReSharper disable once CheckNamespace
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
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (threadName.IsNullOrEmpty()) throw new ArgumentNullException(threadName);

            CountOfItemsProcessed = registry.GetRawValueCounter(Category, "CountOfItemsProcessed", threadName);
            RateOfItemsPerSecondProcessed = registry.GetRateOfCountsPerSecondCounter(Category, "RateOfItemsPerSecondProcessed", threadName);
            AverageRateOfItems = registry.GetAverageValueCounter(Category, "AverageRateOfItems", threadName);
            CountOfItemsProcessedThatSucceed = registry.GetRawValueCounter(Category, "CountOfItemsProcessedThatSucceed", threadName);
            RateOfItemsPerSecondProcessedThatSucceed = registry.GetRateOfCountsPerSecondCounter(Category, "RateOfItemsPerSecondProcessedThatSucceed", threadName);
            AverageRateOfItemsThatSucceed = registry.GetAverageValueCounter(Category, "AverageRateOfItemsThatSucceed", threadName);
            CountOfItemsProcessedThatFail = registry.GetRawValueCounter(Category, "CountOfItemsProcessedThatFail", threadName);
            RateOfItemsPerSecondProcessedThatFail = registry.GetRateOfCountsPerSecondCounter(Category, "RateOfItemsPerSecondProcessedThatFail", threadName);
            AverageRateOfItemsThatFail = registry.GetAverageValueCounter(Category, "AverageRateOfItemsThatFail", threadName);
        }

        private const string Category = "MFDLabs.Concurrency.TaskThread.PerfmonCountersV4";
    }
}
