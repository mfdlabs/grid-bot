using System;

namespace MFDLabs.Sentinels
{
    public class TogglableServiceSentinel : ServiceSentinel
    {
        public bool IsRunning { get; private set; }

        public TogglableServiceSentinel(Func<bool> healthChecker, Func<TimeSpan> monitorIntervalGetter, bool isHealthy = true)
            : base(healthChecker, monitorIntervalGetter, isHealthy)
        {
            IsRunning = true;
        }

        public void StopSentinel()
        {
            if (IsRunning)
            {
                _MonitorTimer.Change(-1, -1);
                IsRunning = false;
            }
        }

        public void StartSentinel()
        {
            if (!IsRunning)
            {
                var monitorInterval = _MonitorIntervalGetter();
                _MonitorTimer.Change(monitorInterval, monitorInterval);
                IsRunning = true;
            }
        }
    }
}
