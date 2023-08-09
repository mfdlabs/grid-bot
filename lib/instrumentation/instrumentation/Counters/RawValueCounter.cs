using System.Threading;
using MFDLabs.Instrumentation.PrometheusListener;

namespace MFDLabs.Instrumentation
{
    internal class RawValueCounter : CounterBase, IRawValueCounter
    {
        public long RawValue => _Value;

        public RawValueCounter(string category, string name, string instance)
            : base(category, name, instance) 
            => _GaugeWrapper = new GaugeWrapper(name, instance, category, PrometheusConstants.RawValue);

        public void Set(long value) => _Value = value;
        public void IncrementBy(long value) => Interlocked.Add(ref _Value, value);
        public void Increment() => Interlocked.Increment(ref _Value);
        public void Decrement() => Interlocked.Decrement(ref _Value);
        internal override double Flush()
        {
            Interlocked.Exchange(ref LastFlushValue, _Value);
            _GaugeWrapper.Set(_Value);
            return _Value;
        }
        internal override double Get() => _Value;

        private long _Value;
        private readonly GaugeWrapper _GaugeWrapper;
    }
}
