using System;
using System.Diagnostics;

namespace MFDLabs.Instrumentation
{
    public class DurationRecorder : IDurationRecorder
    {
        public DurationRecorder(Func<Stopwatch, double> watchReader) 
            => _WatchReader = watchReader ?? throw new ArgumentNullException("watchReader");

        public static DurationRecorder CreateWithMillisecondWatchReader()
            => new DurationRecorder((Stopwatch watch) => watch.Elapsed.TotalMilliseconds);
        public void RecordDuration(Action operation, IAverageValueCounter counter)
        {
            var sw = Stopwatch.StartNew();
            operation();
            sw.Stop();
            counter.Sample(_WatchReader(sw));
        }
        public T RecordDuration<T>(Func<T> operation, IAverageValueCounter counter)
        {
            var sw = Stopwatch.StartNew();
            T result = operation();
            sw.Stop();
            counter.Sample(_WatchReader(sw));
            return result;
        }
        public void RecordDuration(Action operation, IPercentileCounter counter)
        {
            var sw = Stopwatch.StartNew();
            operation();
            sw.Stop();
            counter.Sample(_WatchReader(sw));
        }
        public T RecordDuration<T>(Func<T> operation, IPercentileCounter counter)
        {
            var sw = Stopwatch.StartNew();
            T result = operation();
            sw.Stop();
            counter.Sample(_WatchReader(sw));
            return result;
        }

        private readonly Func<Stopwatch, double> _WatchReader;
    }
}
