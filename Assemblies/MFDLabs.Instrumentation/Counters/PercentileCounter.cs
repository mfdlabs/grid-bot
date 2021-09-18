using MFDLabs.Instrumentation.PrometheusListener;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MFDLabs.Instrumentation
{
    internal class PercentileCounter : IPercentileCounter
    {
        public PercentileCounter(IDictionary<byte, CounterKey> counterKeysByPercentile)
        {
            _CounterKeysByPercentile = counterKeysByPercentile;
            _Values = new ConcurrentBag<double>();
        }

        public PercentileCounter(IDictionary<byte, CounterKey> counterKeysByPercentile, string name, string instance, string category)
            : this(counterKeysByPercentile)
        {
            _Summary = new SummaryWrapper(name, instance, category, PrometheusConstants.Percentile, counterKeysByPercentile.Keys.ToArray());
        }

        public void Sample(double value)
        {
            _Values.Add(value);
            _Summary.AddDataPoint(value);
        }

        internal IReadOnlyCollection<KeyValuePair<CounterKey, double>> Flush()
        {
            var refBag = new ConcurrentBag<double>();
            var exchangedBag = Interlocked.Exchange(ref _Values, refBag);
            if (exchangedBag.IsEmpty)
            {
                return Array.Empty<KeyValuePair<CounterKey, double>>();
            }
            var flushedPercentileCounters = exchangedBag.ToList();
            flushedPercentileCounters.Sort();
            return ComputePercentiles(flushedPercentileCounters);
        }

        internal IReadOnlyCollection<KeyValuePair<CounterKey, double>> Get()
        {
            var percentileCounters = _Values.ToList();
            if (percentileCounters.Count == 0)
            {
                return Array.Empty<KeyValuePair<CounterKey, double>>();
            }
            percentileCounters.Sort();
            return ComputePercentiles(percentileCounters);
        }

        internal static double GetPercentile(IList<double> sortedValues, double percentile)
        {
            if (sortedValues.Count == 0)
            {
                throw new ArgumentException(string.Format("{0} count must be > 0", "sortedValues"), "sortedValues");
            }
            var calculatedPercentile = percentile * (sortedValues.Count - 1);
            var index = (int)calculatedPercentile;
            var switchBackPercentile = calculatedPercentile - index;
            if (index + 1 < sortedValues.Count)
            {
                return sortedValues[index] * (1.0 - switchBackPercentile) + sortedValues[index + 1] * switchBackPercentile;
            }
            return sortedValues[index];
        }

        internal static void ValidatePercentiles(byte[] percentiles)
        {
            if (percentiles == null || percentiles.Length == 0)
            {
                throw new ArgumentException("Percentiles cannot be null or empty", "percentiles");
            }
            foreach (byte percentile in percentiles)
            {
                if (percentile < 1 || percentile > 99)
                {
                    throw new ArgumentException("Percentiles cannot be < 1 or > 99.  The value was " + percentile, "percentiles");
                }
            }
        }

        internal IReadOnlyCollection<KeyValuePair<CounterKey, double>> ComputePercentiles(IList<double> sortedValues)
        {
            var percentileKeys = new List<KeyValuePair<CounterKey, double>>(_CounterKeysByPercentile.Count);
            foreach (var percentileKey in _CounterKeysByPercentile)
            {
                percentileKeys.Add(new KeyValuePair<CounterKey, double>(percentileKey.Value, GetPercentile(sortedValues, percentileKey.Key / 100.0)));
            }
            return percentileKeys;
        }

        private readonly IDictionary<byte, CounterKey> _CounterKeysByPercentile;

        private ConcurrentBag<double> _Values;

        private readonly SummaryWrapper _Summary;
    }
}
