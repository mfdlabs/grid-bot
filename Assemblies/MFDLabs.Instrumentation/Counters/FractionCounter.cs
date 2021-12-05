using System.Threading;
using MFDLabs.Instrumentation.PrometheusListener;

namespace MFDLabs.Instrumentation
{
    internal class FractionCounter : CounterBase, IFractionCounter
    {
        public FractionCounter(string category, string name, string instance)
            : base(category, name, instance)
            => _GaugeWrapper = new GaugeWrapper(name, instance, category, PrometheusConstants.Fraction);

        public void Increment() => Interlocked.Increment(ref _Value);
        public void IncrementBase() => Interlocked.Increment(ref _BaseValue);
        internal override double Flush()
        {
            long persistedValue = _Value;
            long persistedBaseValue = _BaseValue;
            _BaseValue = 0;
            _Value = 0;
            double calculatedFlushResult = (persistedBaseValue == 0) ? 0 : (persistedValue / persistedBaseValue * 100);
            Interlocked.Exchange(ref LastFlushValue, calculatedFlushResult);
            _GaugeWrapper.Set(calculatedFlushResult);
            return calculatedFlushResult;
        }
        internal override double Get()
        {
            double result;
            lock (_Sync) result = (_BaseValue == 0L) ? 0 : (_Value / _BaseValue * 100);
            return result;
        }

        private readonly object _Sync = new object();
        private long _Value;
        private long _BaseValue;
        private readonly GaugeWrapper _GaugeWrapper;
    }
}
