using System;
using System.Diagnostics;
using MFDLabs.Threading;

namespace MFDLabs.Logging
{
#if NET5_0_OR_GREATER
    using System.Runtime.Versioning;

    [SupportedOSPlatform("windows")]
#endif
    public class EventLogLogger : Logger
    {
        private new static EventLogLogger _singleton;

        private EventLog _eventLog;
        private readonly object _eventLogLock = new();

        private bool _logToEventLog = true;
        private Atomic _eventId = 0;

        private EventLogEntryType _logLevelToEntryType(LogLevel logLevel)
            => logLevel switch
            {
                LogLevel.Error => EventLogEntryType.Error,
                LogLevel.Warning => EventLogEntryType.Warning,
                LogLevel.Information => EventLogEntryType.Information,
                LogLevel.Verbose => EventLogEntryType.Information,
                LogLevel.LifecycleEvent => EventLogEntryType.Information,
                _ => EventLogEntryType.Information
            };

        private void _logEventLog(LogLevel logLevel, string format, params object[] args)
        {
            if (!_logToEventLog) return;

            lock (_eventLogLock)
            {
                _eventId++;

                var entryType = _logLevelToEntryType(logLevel);
                var message = _constructLoggerMessage(logLevel, format, args);

                try
                {
                    _eventLog?.WriteEntry(message, entryType, _eventId);
                }
                catch (Exception)
                {
                    _logToEventLog = false;
                }
            }
        }

        protected override void _log(LogLevel logLevel, LogColor color, string format, params object[] args)
        {
            base._log(logLevel, color, format, args);
            _logEventLog(logLevel, format, args);
        }

        /// <summary>
        /// Determines if this logger has an EventLog instance.
        /// </summary>
        /// <returns>True if this logger has an EventLog instance.</returns>
        public bool HasEventLog() => _eventLog != null;

        /// <summary>
        /// Sets the EventLog instance to use.
        /// </summary>
        /// <param name="eventLog">The EventLog instance to use.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="eventLog"/> is null.</exception>
        public void SetEventLog(EventLog eventLog)
        {
            _eventLog = eventLog ?? throw new ArgumentNullException(nameof(eventLog));

            _name = _eventLog.Log.ToLower();
        }

        /// <summary>
        /// Constructs a new instance of the <see cref="EventLogLogger"/> class.
        /// </summary>
        /// <param name="logName">The event log name.</param>
        /// <param name="logSource">The event log source.</param>
        /// <param name="logLevel">The log level of the logger.</param>
        /// <param name="logToFileSystem">If true, the logger will log to the file system.</param>
        /// <param name="logToConsole">If true, the logger will log to the console.</param>
        /// <param name="cutLogPrefix">If true, the logger will cut the log prefix.</param>
        /// <param name="logThreadId">If true, the logger will log with a thread id.</param>
        /// <param name="logWithColor">If true, the logger will console log with colors.</param>
        public EventLogLogger(
            string logName,
            string logSource,
            LogLevel logLevel = LogLevel.Information,
            bool logToFileSystem = true,
            bool logToConsole = true,
            bool cutLogPrefix = true,
            bool logThreadId = false,
            bool logWithColor = false
        ) : this(
                new EventLog(logName, ".", logSource),
                logLevel,
                logToFileSystem,
                logToConsole,
                cutLogPrefix,
                logThreadId,
                logWithColor
            )
        { }

        /// <summary>
        /// Constructs a new instance of the <see cref="EventLogLogger"/> class.
        /// </summary>
        /// <param name="eventLog">The event log to log to.</param>
        /// <param name="logLevel">The log level of the logger.</param>
        /// <param name="logToFileSystem">If true, the logger will log to the file system.</param>
        /// <param name="logToConsole">If true, the logger will log to the console.</param>
        /// <param name="cutLogPrefix">If true, the logger will cut the log prefix.</param>
        /// <param name="logThreadId">If true, the logger will log with a thread id.</param>
        /// <param name="logWithColor">If true, the logger will console log with colors.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="eventLog"/> is null.</exception>
        public EventLogLogger(
            EventLog eventLog,
            LogLevel logLevel = LogLevel.Information,
            bool logToFileSystem = true,
            bool logToConsole = true,
            bool cutLogPrefix = true,
            bool logThreadId = false,
            bool logWithColor = false
        ) : base(
                eventLog?.Log?.ToLower(),
                logLevel,
                logToFileSystem,
                logToConsole,
                cutLogPrefix,
                logThreadId,
                logWithColor
            )
        {
            _eventLog = eventLog ?? throw new ArgumentNullException(nameof(eventLog));
        }

        /// <inheritdoc cref="Logger.Singleton"/>
        public new static EventLogLogger Singleton
            => _singleton ??= new EventLogLogger(
                    LogInstaller.LogName,
                    LogInstaller.SourceName,
                    global::MFDLabs.Logging.Properties.Settings.Default.DefaultLogLevel,
                    global::MFDLabs.Logging.Properties.Settings.Default.DefaultLoggerLogToFileSystem,
                    global::MFDLabs.Logging.Properties.Settings.Default.DefaultLoggerLogToConsole,
                    global::MFDLabs.Logging.Properties.Settings.Default.DefaultLoggerCutLogPrefix,
                    global::MFDLabs.Logging.Properties.Settings.Default.DefaultLoggerLogThreadId,
                    global::MFDLabs.Logging.Properties.Settings.Default.DefaultLoggerLogWithColor
                );
    }
}
