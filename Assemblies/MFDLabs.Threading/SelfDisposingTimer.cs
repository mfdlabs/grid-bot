using System;
using System.Threading;

namespace MFDLabs.Threading
{
    public class SelfDisposingTimer
    {
        public SelfDisposingTimer(Action action, TimeSpan startTime, TimeSpan period)
        {
            _act = action;
            _period = period;
            _timer = new Timer((weakThis) => OnTimer((WeakReference)weakThis), new WeakReference(this), startTime, period);
        }

        private static void OnTimer(WeakReference self)
        {
            if (!(self.Target is SelfDisposingTimer currentTimer))
            {
                return;
            }
            currentTimer._act();
        }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            _period = period;
            return _timer.Change(dueTime, period);
        }

        public void Stop()
        {
            _timer.Dispose();
            _timer = null;
        }

        ~SelfDisposingTimer()
        {
            _timer?.Dispose();
        }

        public void Pause()
        {
            _timer.Change(-1, -1);
        }

        public void Unpause()
        {
            _timer.Change(_period, _period);
        }

        private readonly Action _act;
        private Timer _timer;
        private TimeSpan _period;
    }
}
