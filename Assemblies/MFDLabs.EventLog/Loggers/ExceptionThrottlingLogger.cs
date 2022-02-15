using System;
using System.Collections.Concurrent;
using System.Threading;

namespace MFDLabs.EventLog
{
    public class ExceptionThrottlingLogger : ILogger
    {
        public Func<LogLevel> MaxLogLevel
        {
            get => _logger.MaxLogLevel;
            set => _logger.MaxLogLevel = value;
        }
        public bool LogThreadId
        {
            get => _logger.LogThreadId;
            set => _logger.LogThreadId = value;
        }

        public ExceptionThrottlingLogger(ILogger logger, Func<int> countBeforeThrottlingGetter, Func<TimeSpan> throttlingIntervalGetter, Func<bool> isThrottlingEnabledGetter = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _countBeforeThrottlingGetter = countBeforeThrottlingGetter ?? throw new ArgumentNullException(nameof(countBeforeThrottlingGetter));
            _throttlingIntervalGetter = throttlingIntervalGetter ?? throw new ArgumentNullException(nameof(throttlingIntervalGetter));
            _exceptionCountsByType = new ConcurrentDictionary<Type, ExpiringCount>();
            _errorCountsByFormat = new ConcurrentDictionary<string, ExpiringCount>();
            _warningCountsByFormat = new ConcurrentDictionary<string, ExpiringCount>();
            _errorCountsByThrottleTag = new ConcurrentDictionary<string, ExpiringCount>();
            _isThrottlingEnabledGetter = isThrottlingEnabledGetter ?? (() => true);
        }

        public bool IsDefaultLog
        {
            set
            {
                if (value) StaticLoggerRegistry.SetLogger(this);
            }
        }

        public void Debug(string format, params object[] args) => _logger.Debug(format, args);
        public void Debug(Func<string> messageGetter) => _logger.Debug(messageGetter);
        public void Error(Exception ex)
        {
            if (_isThrottlingEnabledGetter())
            {
                var countBeforeThrottling = _countBeforeThrottlingGetter();
                var throttlingInterval = _throttlingIntervalGetter();
                var exceptionCountByType = _exceptionCountsByType.GetOrAdd(ex.GetType(), _ => new ExpiringCount(throttlingInterval));
                var resetType = 0;
                if (exceptionCountByType.Expiration < DateTime.UtcNow) 
                    resetType = exceptionCountByType.Reset(throttlingInterval) - countBeforeThrottling;
                var incremental = exceptionCountByType.Increment();
                if (resetType > 0)
                {
                    _logger.Error(
                        "{0}\r\nException Logs Throttled. {1} exceptions of the same type in the last {2} seconds were throttled.",
                        ex,
                        resetType,
                        throttlingInterval.TotalSeconds);
                    return;
                }

                if (incremental > countBeforeThrottling) return;
                _logger.Error(ex);
            }
            else
                _logger.Error(ex);
        }
        public void Error(string format, params object[] args)
        {
            if (_isThrottlingEnabledGetter())
            {
                var countBeforeThrottling = _countBeforeThrottlingGetter();
                var throttlingInterval = _throttlingIntervalGetter();
                var errorCountByFormat = _errorCountsByFormat.GetOrAdd(format, _ => new ExpiringCount(throttlingInterval));
                var resetFormat = 0;
                if (errorCountByFormat.Expiration < DateTime.UtcNow) 
                    resetFormat = errorCountByFormat.Reset(throttlingInterval) - countBeforeThrottling;
                var incremental = errorCountByFormat.Increment();
                if (resetFormat > 0)
                {
                    _logger.Error(
                        "{0}\r\nError Logs Throttled. {1} errors of the same format in the last {2} seconds were throttled.",
                        format,
                        resetFormat,
                        throttlingInterval.TotalSeconds);
                    return;
                }

                if (incremental > countBeforeThrottling) return;
                _logger.Error(format, args);
            }
            else
                _logger.Error(format, args);
        }
        public void Error(Func<string> messageGetter) => _logger.Error(messageGetter);
        public void Error(string throttleTag, Func<string> messageGetter)
        {
            if (_isThrottlingEnabledGetter())
            {
                if (throttleTag == null) throw new ArgumentNullException(nameof(throttleTag));
                var countBeforeThrottling = _countBeforeThrottlingGetter();
                var throttlingInterval = _throttlingIntervalGetter();
                var errorCountByThrottleTag = _errorCountsByThrottleTag.GetOrAdd(throttleTag, _ => new ExpiringCount(throttlingInterval));
                var resetThrottleTag = 0;
                if (errorCountByThrottleTag.Expiration < DateTime.UtcNow)
                    resetThrottleTag = errorCountByThrottleTag.Reset(throttlingInterval) - countBeforeThrottling;
                var incremental = errorCountByThrottleTag.Increment();
                if (resetThrottleTag > 0)
                {
                    _logger.Error(
                        "{0}\r\nError Logs Throttled. {1} errors of the same format in the last {2} seconds were throttled.",
                        throttleTag,
                        resetThrottleTag,
                        throttlingInterval.TotalSeconds);
                    return;
                }

                if (incremental > countBeforeThrottling) return;
                _logger.Error(messageGetter);
            }
            else
                _logger.Error(messageGetter);
        }
        public void Info(string format, params object[] args) => _logger.Info(format, args);
        public void Info(Func<string> messageGetter) => _logger.Info(messageGetter);
        public void Log(string format, params object[] args) => _logger.Log(format, args);
        public void Log(Func<string> messageGetter) => _logger.Log(messageGetter);
        public void Warning(string format, params object[] args)
        {
            if (_isThrottlingEnabledGetter())
            {
                var countBeforeThrottling = _countBeforeThrottlingGetter();
                var throttlingInterval = _throttlingIntervalGetter();
                var warningCountByFormat = _warningCountsByFormat.GetOrAdd(format, _ => new ExpiringCount(throttlingInterval));
                var resetFormat = 0;
                if (warningCountByFormat.Expiration < DateTime.UtcNow)
                    resetFormat = warningCountByFormat.Reset(throttlingInterval) - countBeforeThrottling;
                var incremental = warningCountByFormat.Increment();
                if (resetFormat > 0)
                {
                    _logger.Warning(
                        "{0}\r\nWarning Logs Throttled. {1} warnings of the same format in the last {2} seconds were throttled.",
                        format,
                        resetFormat,
                        throttlingInterval.TotalSeconds);
                    return;
                }

                if (incremental > countBeforeThrottling) return;
                _logger.Warning(format, args);
            }
            else
                _logger.Warning(format, args);
        }
        public void Warning(Func<string> messageGetter) => _logger.Warning(messageGetter);
        public void Trace(string format, params object[] args)
        {
            if (_isThrottlingEnabledGetter())
            {
                var countBeforeThrottling = _countBeforeThrottlingGetter();
                var throttlingInterval = _throttlingIntervalGetter();
                var warningCountByFormat = _warningCountsByFormat.GetOrAdd(format, _ => new ExpiringCount(throttlingInterval));
                var resetFormat = 0;
                if (warningCountByFormat.Expiration < DateTime.UtcNow)
                    resetFormat = warningCountByFormat.Reset(throttlingInterval) - countBeforeThrottling;
                var incremental = warningCountByFormat.Increment();
                if (resetFormat > 0)
                {
                    _logger.Warning(
                        "{0}\r\nWarning Logs Throttled. {1} warnings of the same format in the last {2} seconds were throttled.",
                        format,
                        resetFormat,
                        throttlingInterval.TotalSeconds);
                    return;
                }

                if (incremental > countBeforeThrottling) return;
                _logger.Warning(format, args);
            }
            else
                _logger.Warning(format, args);
        }
        public void Trace(Func<string> messageGetter) => _logger.Warning(messageGetter);
        public void Verbose(string format, params object[] args) => _logger.Verbose(format, args);
        public void Verbose(Func<string> messageGetter) => _logger.Verbose(messageGetter);
        public void LifecycleEvent(string format, params object[] args) => _logger.LifecycleEvent(format, args);
        public void LifecycleEvent(Func<string> messageGetter) => _logger.LifecycleEvent(messageGetter);

        private readonly ILogger _logger;
        private readonly Func<int> _countBeforeThrottlingGetter;
        private readonly Func<TimeSpan> _throttlingIntervalGetter;
        private readonly Func<bool> _isThrottlingEnabledGetter;
        private readonly ConcurrentDictionary<Type, ExpiringCount> _exceptionCountsByType;
        private readonly ConcurrentDictionary<string, ExpiringCount> _errorCountsByFormat;
        private readonly ConcurrentDictionary<string, ExpiringCount> _warningCountsByFormat;
        private readonly ConcurrentDictionary<string, ExpiringCount> _errorCountsByThrottleTag;

        private class ExpiringCount
        {
            public ExpiringCount(TimeSpan throttlingInterval)
            {
                Expiration = DateTime.UtcNow.Add(throttlingInterval);
            }

            public int Reset(TimeSpan throttlingInterval)
            {
                Expiration = DateTime.UtcNow.Add(throttlingInterval);
                return Interlocked.Exchange(ref _count, 0);
            }
            public int Increment() => Interlocked.Increment(ref _count);

            private int _count;
            public DateTime Expiration;
        }
    }
}
