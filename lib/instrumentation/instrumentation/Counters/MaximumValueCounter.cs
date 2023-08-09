using System;
using System.Threading;
using MFDLabs.Instrumentation.PrometheusListener;

namespace MFDLabs.Instrumentation
{
    internal class MaximumValueCounter : CounterBase, IMaximumValueCounter
    {
        public MaximumValueCounter(string category, string name, string instance)
            : base(category, name, instance)
            => _GaugeWrapper = new GaugeWrapper(name, instance, category, PrometheusConstants.MaximumValue);

        public void Sample(double value)
        {
            double refValue = _Value;
            double switchKey;
            do
            {
                double maxRef = Math.Max(value, refValue);
                switchKey = refValue;
                refValue = Interlocked.CompareExchange(ref _Value, maxRef, switchKey);
            }
            while (refValue != switchKey);
        }
        internal override double Flush()
        {
            double refValue = Interlocked.Exchange(ref _Value, 0);
            Interlocked.Exchange(ref LastFlushValue, refValue);
            _GaugeWrapper.Set(refValue);
            return refValue;
        }
        internal override double Get() => _Value;

        private double _Value;
        private readonly GaugeWrapper _GaugeWrapper;
    }
}
