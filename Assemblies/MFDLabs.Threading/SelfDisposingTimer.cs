using System;
using System.Threading;

namespace MFDLabs.Threading
{
    public class SelfDisposingTimer
    {
        public SelfDisposingTimer(Action action, TimeSpan startTime, TimeSpan period)
        {
            _Action = action;
            _Period = period;
            _Timer = new Timer(delegate (object weakThis)
            {
                OnTimer((WeakReference)weakThis);
            }, new WeakReference(this), startTime, period);
        }

        private static void OnTimer(WeakReference self)
        {
            if (!(self.Target is SelfDisposingTimer currentTimer))
            {
                return;
            }
            currentTimer._Action();
        }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            _Period = period;
            return _Timer.Change(dueTime, period);
        }

        public void Stop()
        {
            _Timer.Dispose();
            _Timer = null;
        }

        ~SelfDisposingTimer()
        {
            _Timer?.Dispose();
        }

        internal void Pause()
        {
            _Timer.Change(-1, -1);
        }

        internal void Unpause()
        {
            _Timer.Change(_Period, _Period);
        }

        private readonly Action _Action;

        private Timer _Timer;

        private TimeSpan _Period;
    }
}
