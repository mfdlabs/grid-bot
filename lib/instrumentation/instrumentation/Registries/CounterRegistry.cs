using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Instrumentation
{
    public class CounterRegistry : ICounterRegistry
    {
        public IRateOfCountsPerSecondCounter GetRateOfCountsPerSecondCounter(string category, string name, string instance = null)
        {
            return _RateOfCountsPerSecondCounters.GetOrAdd(
                new CounterKey(category, name, instance),
                (key) => new RateOfCountsPerSecondCounter(category, name, instance)
            );
        }
        public IAverageValueCounter GetAverageValueCounter(string category, string name, string instance = null)
        {
            return _AverageValueCounters.GetOrAdd(
                new CounterKey(category, name, instance),
                (key) => new AverageValueCounter(category, name, instance)
            );
        }
        public IMaximumValueCounter GetMaximumValueCounter(string category, string name, string instance = null)
        {
            return _MaximumValueCounters.GetOrAdd(
                new CounterKey(category, name, instance),
                (key) => new MaximumValueCounter(category, name, instance)
            );
        }
        public IRawValueCounter GetRawValueCounter(string category, string name, string instance = null)
        {
            return _RawValueCounters.GetOrAdd(
                new CounterKey(category, name, instance),
                (key) => new RawValueCounter(category, name, instance)
            );
        }
        public IFractionCounter GetFractionCounter(string category, string name, string instance = null)
        {
            return _FractionCounters.GetOrAdd(
                new CounterKey(category, name, instance),
                (key) => new FractionCounter(category, name, instance)
            );
        }
        public IPercentileCounter GetPercentileCounter(string category, string nameFormatString, byte[] percentiles, string instance = null)
        {
            ValidateStringParameter(category, "category");
            ValidateStringParameter(nameFormatString, "nameFormatString");
            PercentileCounter.ValidatePercentiles(percentiles);
            var key = string.Format(
                "category={0}|nameformatstring={1}|instance={2}|{3}",
                category,
                nameFormatString,
                instance,
                Convert.ToBase64String(percentiles)
            );
            return _PercentileCounters.GetOrAdd(key, _ =>
            {
                var counterKeys = new Dictionary<byte, CounterKey>();
                foreach (byte percentile in percentiles)
                    counterKeys[percentile] = new CounterKey(category, string.Format(nameFormatString, percentile.ToString("D2")), instance);
                return new PercentileCounter(counterKeys, nameFormatString, instance, category);
            });
        }
        public IPercentileCounter GetPercentileCounter(string category, string name, string instanceFormatString, byte[] percentiles)
        {
            ValidateStringParameter(category, "category");
            ValidateStringParameter(name, "name");
            ValidateStringParameter(instanceFormatString, "instanceFormatString");
            PercentileCounter.ValidatePercentiles(percentiles);
            var key = string.Format(
                "category={0}|name={1}|instanceFormatString={2}|{3}",
                category,
                name,
                instanceFormatString,
                Convert.ToBase64String(percentiles)
            );
            return _PercentileCounters.GetOrAdd(key, k =>
            {
                var counterKeys = new Dictionary<byte, CounterKey>();
                foreach (byte percentile in percentiles)
                {
                    string instance = string.Format(instanceFormatString, percentile.ToString("D2"));
                    CounterKey value = new CounterKey(category, name, instance);
                    counterKeys[percentile] = value;
                }
                return new PercentileCounter(counterKeys, name, instanceFormatString, category);
            });
        }
        public IReadOnlyCollection<KeyValuePair<CounterKey, double>> FlushCounters()
        {
            var counters = new List<KeyValuePair<CounterKey, double>>();
            foreach (var counterKey in GetAllRegisteredCounters()) 
                counters.Add(new KeyValuePair<CounterKey, double>(counterKey.Key, counterKey.Flush()));
            foreach (var percentileCounterKey in _PercentileCounters)
                counters.AddRange(percentileCounterKey.Value.Flush());
            return counters;
        }
        public IReadOnlyCollection<KeyValuePair<CounterKey, double>> GetCounterValues()
        {
            var counters = new List<KeyValuePair<CounterKey, double>>();
            foreach (var counterKey in GetAllRegisteredCounters()) 
                counters.Add(new KeyValuePair<CounterKey, double>(counterKey.Key, counterKey.Get()));
            foreach (var percentileCounterKey in _PercentileCounters) 
                counters.AddRange(percentileCounterKey.Value.Get());
            return counters;
        }
        public IReadOnlyCollection<byte> GetDefaultPercentiles() => _DefaultPercentiles;
        private IEnumerable<CounterBase> GetAllRegisteredCounters()
        {
            foreach (var rateOfCountPerSecondCounterKey in _RateOfCountsPerSecondCounters) yield return rateOfCountPerSecondCounterKey.Value;
            foreach (var averageValueCounterKey in _AverageValueCounters) yield return averageValueCounterKey.Value;
            foreach (var maxValueCounterKey in _MaximumValueCounters) yield return maxValueCounterKey.Value;
            foreach (var rawValueCounterKey in _RawValueCounters) yield return rawValueCounterKey.Value;
            foreach (var fractionCounterKey in _FractionCounters) yield return fractionCounterKey.Value;
            yield break;
        }
        private void ValidateStringParameter(string parameter, string parameterName)
        {
            if (parameter.IsNullOrWhiteSpace()) throw new ArgumentException(parameterName);
        }

        private static readonly byte[] _DefaultPercentiles = new byte[] { 1, 5, 10, 25, 50, 75, 90, 95, 99 };
        private readonly ConcurrentDictionary<CounterKey, RateOfCountsPerSecondCounter> _RateOfCountsPerSecondCounters = new ConcurrentDictionary<CounterKey, RateOfCountsPerSecondCounter>();
        private readonly ConcurrentDictionary<CounterKey, AverageValueCounter> _AverageValueCounters = new ConcurrentDictionary<CounterKey, AverageValueCounter>();
        private readonly ConcurrentDictionary<CounterKey, MaximumValueCounter> _MaximumValueCounters = new ConcurrentDictionary<CounterKey, MaximumValueCounter>();
        private readonly ConcurrentDictionary<CounterKey, FractionCounter> _FractionCounters = new ConcurrentDictionary<CounterKey, FractionCounter>();
        private readonly ConcurrentDictionary<CounterKey, RawValueCounter> _RawValueCounters = new ConcurrentDictionary<CounterKey, RawValueCounter>();
        private readonly ConcurrentDictionary<string, PercentileCounter> _PercentileCounters = new ConcurrentDictionary<string, PercentileCounter>();
    }
}
