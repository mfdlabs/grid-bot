using System;
using System.Threading;

namespace MFDLabs.Sentinels
{
    public class ServiceSentinel : ISentinel, IDisposable
    {
        public bool IsHealthy { get; private set; }

        public ServiceSentinel(Func<bool> healthChecker, Func<TimeSpan> monitorIntervalGetter, bool isHealthy = true)
        {
            _healthChecker = healthChecker;
            MonitorIntervalGetter = monitorIntervalGetter;
            IsHealthy = isHealthy;
            MonitorTimer = new Timer(OnTimerCallback);
            var monitorInterval = monitorIntervalGetter();
            MonitorTimer.Change(monitorInterval, monitorInterval);
        }

        private void OnTimerCallback(object state)
        {
            if (_isDisposed) return;
            var currentTimerState = (Timer)state;
            try
            {
                currentTimerState.Change(-1, -1);
                IsHealthy = _healthChecker();
            }
            catch (Exception) { IsHealthy = false; }
            finally
            {
                try
                {
                    var monitorInterval = MonitorIntervalGetter();
                    currentTimerState.Change(monitorInterval, monitorInterval);
                }
                catch
                {
                    // ignored
                }
            }
        }
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            if (disposing)
            {
                MonitorTimer.CheckAndDispose();
                MonitorTimer = null;
            }
            _isDisposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private readonly Func<bool> _healthChecker;
        private bool _isDisposed;
        protected readonly Func<TimeSpan> MonitorIntervalGetter;
        protected Timer MonitorTimer;
    }
}
