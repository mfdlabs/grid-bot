using System.Collections.Generic;

namespace Instrumentation
{
    public sealed class NoOpCounterRegistry : ICounterRegistry
    {
        public IRateOfCountsPerSecondCounter GetRateOfCountsPerSecondCounter(string category, string name, string instance = null) => new NoOpCounter();
        public IAverageValueCounter GetAverageValueCounter(string category, string name, string instance = null) => new NoOpCounter();
        public IMaximumValueCounter GetMaximumValueCounter(string category, string name, string instance = null) => new NoOpCounter();
        public IRawValueCounter GetRawValueCounter(string category, string name, string instance = null) => new NoOpCounter();
        public IFractionCounter GetFractionCounter(string category, string name, string instance = null) => new NoOpCounter();
        public IPercentileCounter GetPercentileCounter(string category, string nameFormatString, byte[] percentiles, string instance = null) => new NoOpCounter();
        public IPercentileCounter GetPercentileCounter(string category, string name, string instanceFormatString, byte[] percentiles) => new NoOpCounter();
        public IReadOnlyCollection<KeyValuePair<CounterKey, double>> FlushCounters() => _EmptyCounterValues;
        public IReadOnlyCollection<KeyValuePair<CounterKey, double>> GetCounterValues() => _EmptyCounterValues;
        public IReadOnlyCollection<byte> GetDefaultPercentiles() => _DefaultPercentiles;

        private static readonly IReadOnlyCollection<KeyValuePair<CounterKey, double>> _EmptyCounterValues = new KeyValuePair<CounterKey, double>[0];
        private static readonly byte[] _DefaultPercentiles = new byte[0];
    }
}
