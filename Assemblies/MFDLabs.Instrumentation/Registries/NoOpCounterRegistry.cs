using System.Collections.Generic;

namespace MFDLabs.Instrumentation
{
    public sealed class NoOpCounterRegistry : ICounterRegistry
    {
        public IRateOfCountsPerSecondCounter GetRateOfCountsPerSecondCounter(string category, string name, string instance = null)
        {
            return new NoOpCounter();
        }

        public IAverageValueCounter GetAverageValueCounter(string category, string name, string instance = null)
        {
            return new NoOpCounter();
        }

        public IMaximumValueCounter GetMaximumValueCounter(string category, string name, string instance = null)
        {
            return new NoOpCounter();
        }

        public IRawValueCounter GetRawValueCounter(string category, string name, string instance = null)
        {
            return new NoOpCounter();
        }

        public IFractionCounter GetFractionCounter(string category, string name, string instance = null)
        {
            return new NoOpCounter();
        }

        public IPercentileCounter GetPercentileCounter(string category, string nameFormatString, byte[] percentiles, string instance = null)
        {
            return new NoOpCounter();
        }

        public IPercentileCounter GetPercentileCounter(string category, string name, string instanceFormatString, byte[] percentiles)
        {
            return new NoOpCounter();
        }

        public IReadOnlyCollection<KeyValuePair<CounterKey, double>> FlushCounters()
        {
            return _EmptyCounterValues;
        }

        public IReadOnlyCollection<KeyValuePair<CounterKey, double>> GetCounterValues()
        {
            return _EmptyCounterValues;
        }

        public IReadOnlyCollection<byte> GetDefaultPercentiles()
        {
            return _DefaultPercentiles;
        }

        private static readonly IReadOnlyCollection<KeyValuePair<CounterKey, double>> _EmptyCounterValues = new KeyValuePair<CounterKey, double>[0];

        private static readonly byte[] _DefaultPercentiles = new byte[0];
    }
}
