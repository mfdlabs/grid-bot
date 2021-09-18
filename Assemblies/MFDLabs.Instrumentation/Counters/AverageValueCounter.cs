using MFDLabs.Instrumentation.PrometheusListener;
using System.Threading;

namespace MFDLabs.Instrumentation
{
    internal class AverageValueCounter : CounterBase, IAverageValueCounter
    {
        public AverageValueCounter(string category, string name, string instance)
            : base(category, name, instance)
        {
            _GaugeWrapper = new GaugeWrapper(name, instance, category, PrometheusConstants.AverageValue);
        }

        public void Sample(double value)
        {
            lock (_Sync)
            {
                _Sum += value;
                _Count += 1L;
            }
        }

        internal override double Flush()
        {
            double sum;
            long count;
            lock (_Sync)
            {
                sum = _Sum;
                count = _Count;
                _Sum = 0.0;
                _Count = 0L;
            }
            double sumCalc = (count == 0L) ? 0.0 : (sum / count);
            Interlocked.Exchange(ref LastFlushValue, sumCalc);
            _GaugeWrapper.Set(sumCalc);
            return sumCalc;
        }

        internal override double Get()
        {
            double result;
            lock (_Sync)
            {
                result = ((_Count == 0L) ? 0.0 : (_Sum / _Count));
            }
            return result;
        }

        private readonly object _Sync = new object();

        private double _Sum;

        private long _Count;

        private readonly GaugeWrapper _GaugeWrapper;
    }
}
