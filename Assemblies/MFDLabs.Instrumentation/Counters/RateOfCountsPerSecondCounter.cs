using System;
using System.Threading;
using MFDLabs.Instrumentation.PrometheusListener;

namespace MFDLabs.Instrumentation
{
    internal class RateOfCountsPerSecondCounter : CounterBase, IRateOfCountsPerSecondCounter
    {
        public RateOfCountsPerSecondCounter(string category, string name, string instance)
            : base(category, name, instance)
        {
            _LastFlush = DateTime.UtcNow;
            _GaugeWrapper = new GaugeWrapper(name, instance, category, PrometheusConstants.RateOfCountsPerSecond);
        }

        public void IncrementBy(long eventCount)
        {
            Interlocked.Add(ref _NumberOfEvents, eventCount);
        }

        public void Increment()
        {
            IncrementBy(1L);
        }

        internal override double Flush()
        {
            var now = DateTime.UtcNow;
            var exchangedNumberOfEvents = Interlocked.Exchange(ref _NumberOfEvents, 0L);
            var totalSeconds = (now - _LastFlush).TotalSeconds;
            _LastFlush = now;
            if (totalSeconds == 0.0)
            {
                _GaugeWrapper.Set(0.0);
                return 0.0;
            }
            double lastFlushValue = exchangedNumberOfEvents / totalSeconds;
            Interlocked.Exchange(ref LastFlushValue, lastFlushValue);
            _GaugeWrapper.Set(lastFlushValue);
            return lastFlushValue;
        }

        internal override double Get()
        {
            var totalSeconds = (DateTime.UtcNow - _LastFlush).TotalSeconds;
            if (totalSeconds == 0.0)
            {
                return 0.0;
            }
            return _NumberOfEvents / totalSeconds;
        }

        private long _NumberOfEvents;

        private DateTime _LastFlush;

        private readonly GaugeWrapper _GaugeWrapper;
    }
}
