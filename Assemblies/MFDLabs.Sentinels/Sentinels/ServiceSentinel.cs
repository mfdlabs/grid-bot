using System;
using System.Threading;

namespace MFDLabs.Sentinels
{
    public class ServiceSentinel : ISentinel, IDisposable
    {
        public bool IsHealthy { get; private set; }

        public ServiceSentinel(Func<bool> healthChecker, Func<TimeSpan> monitorIntervalGetter, bool isHealthy = true)
        {
            _HealthChecker = healthChecker;
            _MonitorIntervalGetter = monitorIntervalGetter;
            IsHealthy = isHealthy;
            _MonitorTimer = new Timer(OnTimerCallback);
            var monitorInterval = monitorIntervalGetter();
            _MonitorTimer.Change(monitorInterval, monitorInterval);
        }

        private void OnTimerCallback(object state)
        {
            if (_IsDisposed) return;
            var currentTimerState = (Timer)state;
            try
            {
                currentTimerState.Change(-1, -1);
                IsHealthy = _HealthChecker();
            }
            catch (Exception) { IsHealthy = false; }
            finally
            {
                try
                {
                    var monitorInterval = _MonitorIntervalGetter();
                    currentTimerState.Change(monitorInterval, monitorInterval);
                }
                catch
                { }
            }
        }
        protected virtual void Dispose(bool disposing)
        {
            if (_IsDisposed) return;
            if (disposing)
            {
                _MonitorTimer.CheckAndDispose();
                _MonitorTimer = null;
            }
            _IsDisposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private readonly Func<bool> _HealthChecker;
        private bool _IsDisposed;
        protected readonly Func<TimeSpan> _MonitorIntervalGetter;
        protected Timer _MonitorTimer;
    }
}
