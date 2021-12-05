using System;
using System.Collections.Concurrent;
using System.Threading;

namespace MFDLabs.EventLog
{
    public class ExceptionThrottlingLogger : ILogger
    {
        public Func<LogLevel> MaxLogLevel
        {
            get => _Logger.MaxLogLevel;
            set => _Logger.MaxLogLevel = value;
        }
        public bool LogThreadID
        {
            get => _Logger.LogThreadID;
            set => _Logger.LogThreadID = value;
        }

        public ExceptionThrottlingLogger(ILogger logger, Func<int> countBeforeThrottlingGetter, Func<TimeSpan> throttlingIntervalGetter, Func<bool> isThrottlingEnabledGetter = null)
        {
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _CountBeforeThrottlingGetter = countBeforeThrottlingGetter ?? throw new ArgumentNullException(nameof(countBeforeThrottlingGetter));
            _ThrottlingIntervalGetter = throttlingIntervalGetter ?? throw new ArgumentNullException(nameof(throttlingIntervalGetter));
            _ExceptionCountsByType = new ConcurrentDictionary<Type, ExpiringCount>();
            _ErrorCountsByFormat = new ConcurrentDictionary<string, ExpiringCount>();
            _WarningCountsByFormat = new ConcurrentDictionary<string, ExpiringCount>();
            _ErrorCountsByThrottleTag = new ConcurrentDictionary<string, ExpiringCount>();
            _IsThrottlingEnabledGetter = isThrottlingEnabledGetter ?? (() => true);
        }

        public bool IsDefaultLog
        {
            set
            {
                if (value) StaticLoggerRegistry.SetLogger(this);
            }
        }

        public void Debug(string format, params object[] args) => _Logger.Debug(format, args);
        public void Debug(Func<string> messageGetter) => _Logger.Debug(messageGetter);
        public void Error(Exception ex)
        {
            if (_IsThrottlingEnabledGetter())
            {
                var countBeforeThrottling = _CountBeforeThrottlingGetter();
                var throttlingInterval = _ThrottlingIntervalGetter();
                var exceptionCountByType = _ExceptionCountsByType.GetOrAdd(ex.GetType(), (key) => new ExpiringCount(throttlingInterval));
                var resetType = 0;
                if (exceptionCountByType.Expiration < DateTime.UtcNow) 
                    resetType = exceptionCountByType.Reset(throttlingInterval) - countBeforeThrottling;
                var incremental = exceptionCountByType.Increment();
                if (resetType > 0)
                {
                    _Logger.Error("{0}\r\nException Logs Throttled. {1} exceptions of the same type in the last {2} seconds were throttled.", ex, resetType, throttlingInterval.TotalSeconds);
                    return;
                }
                if (incremental <= countBeforeThrottling)
                {
                    _Logger.Error(ex);
                    return;
                }
            }
            else
                _Logger.Error(ex);
        }
        public void Error(string format, params object[] args)
        {
            if (_IsThrottlingEnabledGetter())
            {
                var countBeforeThrottling = _CountBeforeThrottlingGetter();
                var throttlingInterval = _ThrottlingIntervalGetter();
                var errorCountByFormat = _ErrorCountsByFormat.GetOrAdd(format, (key) => new ExpiringCount(throttlingInterval));
                int resetFormat = 0;
                if (errorCountByFormat.Expiration < DateTime.UtcNow) 
                    resetFormat = errorCountByFormat.Reset(throttlingInterval) - countBeforeThrottling;
                int incremental = errorCountByFormat.Increment();
                if (resetFormat > 0)
                {
                    _Logger.Error("{0}\r\nError Logs Throttled. {1} errors of the same format in the last {2} seconds were throttled.", format, resetFormat, throttlingInterval.TotalSeconds);
                    return;
                }
                if (incremental <= countBeforeThrottling)
                {
                    _Logger.Error(format, args);
                    return;
                }
            }
            else
                _Logger.Error(format, args);
        }
        public void Error(Func<string> messageGetter) => _Logger.Error(messageGetter);
        public void Error(string throttleTag, Func<string> messageGetter)
        {
            if (_IsThrottlingEnabledGetter())
            {
                if (throttleTag == null) throw new ArgumentNullException(nameof(throttleTag));
                var countBeforeThrottling = _CountBeforeThrottlingGetter();
                var throttlingInterval = _ThrottlingIntervalGetter();
                var errorCountByThrottleTag = _ErrorCountsByThrottleTag.GetOrAdd(throttleTag, (string key) => new ExpiringCount(throttlingInterval));
                var resetThrottleTag = 0;
                if (errorCountByThrottleTag.Expiration < DateTime.UtcNow)
                    resetThrottleTag = errorCountByThrottleTag.Reset(throttlingInterval) - countBeforeThrottling;
                var incremental = errorCountByThrottleTag.Increment();
                if (resetThrottleTag > 0)
                {
                    _Logger.Error("{0}\r\nError Logs Throttled. {1} errors of the same format in the last {2} seconds were throttled.", throttleTag, resetThrottleTag, throttlingInterval.TotalSeconds);
                    return;
                }
                if (incremental <= countBeforeThrottling)
                {
                    _Logger.Error(messageGetter);
                    return;
                }
            }
            else
                _Logger.Error(messageGetter);
        }
        public void Info(string format, params object[] args) => _Logger.Info(format, args);
        public void Info(Func<string> messageGetter) => _Logger.Info(messageGetter);
        public void Warning(string format, params object[] args)
        {
            if (_IsThrottlingEnabledGetter())
            {
                var countBeforeThrottling = _CountBeforeThrottlingGetter();
                var throttlingInterval = _ThrottlingIntervalGetter();
                var warningCountByFormat = _WarningCountsByFormat.GetOrAdd(format, (string key) => new ExpiringCount(throttlingInterval));
                var resetFormat = 0;
                if (warningCountByFormat.Expiration < DateTime.UtcNow)
                    resetFormat = warningCountByFormat.Reset(throttlingInterval) - countBeforeThrottling;
                var incremental = warningCountByFormat.Increment();
                if (resetFormat > 0)
                {
                    _Logger.Warning("{0}\r\nWarning Logs Throttled. {1} warnings of the same format in the last {2} seconds were throttled.", format, resetFormat, throttlingInterval.TotalSeconds);
                    return;
                }
                if (incremental <= countBeforeThrottling)
                {
                    _Logger.Warning(format, args);
                    return;
                }
            }
            else
                _Logger.Warning(format, args);
        }
        public void Warning(Func<string> messageGetter) => _Logger.Warning(messageGetter);
        public void Verbose(string format, params object[] args) => _Logger.Verbose(format, args);
        public void Verbose(Func<string> messageGetter) => _Logger.Verbose(messageGetter);
        public void LifecycleEvent(string format, params object[] args) => _Logger.LifecycleEvent(format, args);
        public void LifecycleEvent(Func<string> messageGetter) => _Logger.LifecycleEvent(messageGetter);

        private readonly ILogger _Logger;
        private readonly Func<int> _CountBeforeThrottlingGetter;
        private readonly Func<TimeSpan> _ThrottlingIntervalGetter;
        private readonly Func<bool> _IsThrottlingEnabledGetter;
        private readonly ConcurrentDictionary<Type, ExpiringCount> _ExceptionCountsByType;
        private readonly ConcurrentDictionary<string, ExpiringCount> _ErrorCountsByFormat;
        private readonly ConcurrentDictionary<string, ExpiringCount> _WarningCountsByFormat;
        private readonly ConcurrentDictionary<string, ExpiringCount> _ErrorCountsByThrottleTag;

        private class ExpiringCount
        {
            public ExpiringCount(TimeSpan throttlingInterval)
            {
                Expiration = DateTime.UtcNow.Add(throttlingInterval);
            }

            public int Reset(TimeSpan throttlingInterval)
            {
                Expiration = DateTime.UtcNow.Add(throttlingInterval);
                return Interlocked.Exchange(ref Count, 0);
            }
            public int Increment() => Interlocked.Increment(ref Count);

            public int Count;
            public DateTime Expiration;
        }
    }
}
