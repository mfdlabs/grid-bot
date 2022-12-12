namespace MFDLabs.Logging;

using System;
using System.Diagnostics;

/// <summary>
/// Implements an event log supported version of <see cref="Logger"/>
/// </summary>
public class EventLogLogger : Logger
{
    private new static EventLogLogger _singleton;

    private EventLog _eventLog;
    private readonly object _eventLogLock = new();

    private bool _logToEventLog = true;

    private static EventLogEntryType _logLevelToEntryType(LogLevel logLevel)
        => logLevel switch
        {
            LogLevel.Error => EventLogEntryType.Error,
            LogLevel.Warning => EventLogEntryType.Warning,
            LogLevel.Information => EventLogEntryType.Information,
            LogLevel.Verbose => EventLogEntryType.Information,
            LogLevel.LifecycleEvent => EventLogEntryType.Information,
            _ => EventLogEntryType.Information
        };

    private void _writeToEventLog(LogLevel logLevel, string format, params object[] args)
    {
        lock (this._eventLogLock)
        {
            if (!this._logToEventLog) return;

            var entryType = EventLogLogger._logLevelToEntryType(logLevel);
            var message = base._constructNonColorLogMessage(logLevel, format, args);

            try
            {
                this._eventLog?.WriteEntry(message, entryType);
            }
            catch (Exception)
            {
                this._logToEventLog = false;
            }
        }
    }

    /// <inheritdoc cref="Logger._writeLog(LogLevel, LogColor, string, object[])"/>
    protected override void _writeLog(LogLevel logLevel, Logger.LogColor color, string format, params object[] args)
    {
        base._writeLog(logLevel, color, format, args);
        this._writeToEventLog(logLevel, format, args);
    }

    /// <summary>
    /// Determines if this logger has an EventLog instance.
    /// </summary>
    /// <returns>True if this logger has an EventLog instance.</returns>
    public bool HasEventLog() => this._eventLog != null;

    /// <summary>
    /// Sets the EventLog instance to use.
    /// </summary>
    /// <param name="eventLog">The EventLog instance to use.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="eventLog"/> is null.</exception>
    public void SetEventLog(EventLog eventLog)
    {
        this._eventLog = eventLog ?? throw new ArgumentNullException(nameof(eventLog));

        this.Name = this._eventLog.Log.ToLower();
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
        this._eventLog = eventLog ?? throw new ArgumentNullException(nameof(eventLog));
    }

    /// <inheritdoc cref="Logger.Singleton"/>
    public new static EventLogLogger Singleton
        => _singleton ??= new EventLogLogger(
                typeof(EventLogLogger).Namespace,
                typeof(EventLogLogger).Name,
                global::MFDLabs.Logging.Properties.Settings.Default.DefaultLogLevel,
                global::MFDLabs.Logging.Properties.Settings.Default.DefaultLoggerLogToFileSystem,
                global::MFDLabs.Logging.Properties.Settings.Default.DefaultLoggerLogToConsole,
                global::MFDLabs.Logging.Properties.Settings.Default.DefaultLoggerCutLogPrefix,
                global::MFDLabs.Logging.Properties.Settings.Default.DefaultLoggerLogThreadId,
                global::MFDLabs.Logging.Properties.Settings.Default.DefaultLoggerLogWithColor
            );
}
