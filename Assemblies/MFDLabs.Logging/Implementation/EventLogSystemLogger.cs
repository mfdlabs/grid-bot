using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using MFDLabs.Abstractions;
using MFDLabs.Diagnostics;
using MFDLabs.ErrorHandling.Extensions;
using MFDLabs.EventLog;
using MFDLabs.Logging.Diagnostics;
using MFDLabs.Networking;
using MFDLabs.Text;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Logging
{
    [DebuggerDisplay("Global EventLog Logger")]
    [DebuggerStepThrough]
    public sealed class EventLogSystemLogger : SingletonBase<EventLogSystemLogger>, ILogger
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string _localIp = NetworkingGlobal.Singleton.GetLocalIP();
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string _machineId = SystemGlobal.Singleton.GetMachineID();
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string _machineHost = SystemGlobal.Singleton.GetMachineHost();

        private readonly string _fileName =
#if DEBUG
                        "\\dev_log_" +
#else
                        "\\log_" +
#endif
                        $"{DateTimeGlobal.Singleton.GetUtcNowAsISO().MakeFileSafeString()}-{SystemGlobal.Singleton.CurrentProcess.Id:X}.log";

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static bool _CanLog = true;


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private System.Diagnostics.EventLog _eventLog;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _eventId = 0;

        [DebuggerHidden]
        [DebuggerStepThrough]
        public void Initialize(System.Diagnostics.EventLog eventLog)
        {
            _eventLog = eventLog;
        }

        public Func<LogLevel> MaxLogLevel { [DebuggerStepThrough]get; [DebuggerStepThrough]set; } = () => global::MFDLabs.Logging.Properties.Settings.Default.MaxLogLevel;

        [DebuggerHidden]
        public bool LogThreadID { get; set; } = false;

        [DebuggerStepThrough]
        private string ConstructLoggerMessage(string logType, string format, params object[] args)
        {
            var threadID = Thread.CurrentThread.ManagedThreadId.ToString("x");
            var countNCharsToReplace = 4 - threadID.Length;

            var internalMessage = string.Format(
                "[{0}][{1}][{2}][{3}][{4}-{5}][{6}][{7}][{8}][{9}][{10}][{11}][{12}][{13}] {14}\n",
                DateTimeGlobal.Singleton.GetUtcNowAsISO(),
                SystemGlobal.Singleton.CurrentProcess.Id.ToString("x"),
                threadID.Fill('0', countNCharsToReplace, TextGlobal.StringDirection.Left),
                LoggingSystem.Singleton.GlobalLifetimeWatch.Elapsed.TotalSeconds.ToString("f7"),
                SystemGlobal.Singleton.CurrentPlatform,
                SystemGlobal.Singleton.CurrentDeviceArch.ToLower(),
                SystemGlobal.Singleton.Version,
                SystemGlobal.Singleton.AssemblyVersion,
#if DEBUG
                "Debug",
#else
                "Release",
#endif
                _localIp,
                _machineId,
                _machineHost,
                global::MFDLabs.Logging.Properties.Settings.Default.LoggingUtilDataName,
                logType.ToUpper(),
                format
            );

            if (args != null && args.Length > 0)
                return string.Format(internalMessage, args);

            return internalMessage;
        }

        [DebuggerStepThrough]
        private void LogLocally(LogLevel level, string logType, string format, params object[] args)
        {
            if (_CanLog)
            {
                if (level <= MaxLogLevel())
                {
                    var str = ConstructLoggerMessage(logType, format, args);
                    var dirName = $"{Environment.GetEnvironmentVariable("LOCALAPPDATA")}\\MFDLABS\\Logs";

                    if (!Directory.Exists(dirName + "\\..\\"))
                        Directory.CreateDirectory(dirName + "\\..\\");
                    if (!Directory.Exists(dirName))
                        Directory.CreateDirectory(dirName);
                    try { File.AppendAllText(dirName + _fileName, str); } catch { }
                }
            }
        }

        [DebuggerStepThrough]
        public void TryClearLocalLog(bool overrideENV = false, bool wasForGlobalEventLifeTimeClosure = false)
        {
            if (_CanLog)
            {
                Log("Try clear local logs...");

                if (global::MFDLabs.Logging.Properties.Settings.Default.PersistLocalLogs)
                {
                    if (overrideENV)
                    {
                        Warning("Overriding global config when clearing logs.");
                    }
                    else
                    {
                        Warning("The local log is set to persist. Please change Setting \"PersistLocalLogs\" to change this.");
                        _CanLog = wasForGlobalEventLifeTimeClosure != true;
                        return;
                    }
                }

                Log("Clearing LocalLog...");

                var dirName = $"{Environment.GetEnvironmentVariable("LOCALAPPDATA")}\\MFDLabs\\Logs";

                _CanLog = wasForGlobalEventLifeTimeClosure != true;

                if (Directory.Exists(dirName))
                {
                    Directory.Delete(dirName, true);
                    return;
                }
            }
        }

        [DebuggerStepThrough]
        public void Log(string format, params object[] args)
        {
            LogToEventLog(EventLogEntryType.Information, LogLevel.None, "LOG", format, args);
            LogLocally(LogLevel.None, "LOG", format, args);
        }

        [DebuggerStepThrough]
        public void Log(Func<string> messageGetter)
        {
            LogToEventLog(EventLogEntryType.Information, LogLevel.None, "LOG", messageGetter());
            LogLocally(LogLevel.None, "LOG", messageGetter());
        }

        [DebuggerStepThrough]
        public void Warning(string format, params object[] args)
        {
            LogToEventLog(EventLogEntryType.Warning, LogLevel.Warning, "WARNING", format, args);
            LogLocally(LogLevel.Warning, "WARNING", format, args);
        }

        [DebuggerStepThrough]
        public void Warning(Func<string> messageGetter)
        {
            LogToEventLog(EventLogEntryType.Warning, LogLevel.Warning, "WARNING", messageGetter());
            LogLocally(LogLevel.Warning, "WARNING", messageGetter());
        }

        [DebuggerStepThrough]
        public void Trace(string format, params object[] args)
        {
            LogToEventLog(EventLogEntryType.Error, LogLevel.Error, "TRACE", format, args);
            LogLocally(LogLevel.Error, "TRACE", format, args);
        }

        [DebuggerStepThrough]
        public void Trace(Func<string> messageGetter)
        {
            LogToEventLog(EventLogEntryType.Error, LogLevel.Error, "TRACE", messageGetter());
            LogLocally(LogLevel.Error, "TRACE", messageGetter());
        }

        [DebuggerStepThrough]
        public void Debug(string format, params object[] args)
        {
            LogToEventLog(EventLogEntryType.Warning, LogLevel.Verbose, "DEBUG", format, args);
            LogLocally(LogLevel.Verbose, "DEBUG", format, args);
        }

        [DebuggerStepThrough]
        public void Debug(Func<string> messageGetter)
        {
            LogToEventLog(EventLogEntryType.Warning, LogLevel.Verbose, "DEBUG", messageGetter());
            LogLocally(LogLevel.Verbose, "DEBUG", messageGetter());
        }

        [DebuggerStepThrough]
        public void Info(string format, params object[] args)
        {
            LogToEventLog(EventLogEntryType.Information, LogLevel.Information, "INFO", format, args);
            LogLocally(LogLevel.Information, "INFO", format, args);
        }

        [DebuggerStepThrough]
        public void Info(Func<string> messageGetter)
        {
            LogToEventLog(EventLogEntryType.Information, LogLevel.Information, "INFO", messageGetter());
            LogLocally(LogLevel.Information, "INFO", messageGetter());
        }

        [DebuggerStepThrough]
        public void Error(string format, params object[] args)
        {
            LogToEventLog(EventLogEntryType.Error, LogLevel.Error, "ERROR", format, args);
            LogLocally(LogLevel.Error, "ERROR", format, args);
        }

        [DebuggerStepThrough]
        public void Error(Exception ex)
        {
            LogToEventLog(EventLogEntryType.Error, LogLevel.Error, "ERROR", ex.ToDetailedString());
            LogLocally(LogLevel.Error, "ERROR", ex.ToDetailedString());
        }

        [DebuggerStepThrough]
        public void Error(Func<string> messageGetter)
        {
            LogToEventLog(EventLogEntryType.Error, LogLevel.Error, "ERROR", messageGetter());
            LogLocally(LogLevel.Error, "ERROR", messageGetter());
        }

        [DebuggerStepThrough]
        public void Verbose(string format, params object[] args)
        {
            LogToEventLog(EventLogEntryType.Warning, LogLevel.Verbose, "VERBOSE", format, args);
            LogLocally(LogLevel.Verbose, "VERBOSE", format, args);
        }

        [DebuggerStepThrough]
        public void Verbose(Func<string> messageGetter)
        {
            LogToEventLog(EventLogEntryType.Warning, LogLevel.Verbose, "VERBOSE", messageGetter());
            LogLocally(LogLevel.Verbose, "VERBOSE", messageGetter());
        }

        [DebuggerStepThrough]
        public void LifecycleEvent(string format, params object[] args)
        {
            LogToEventLog(EventLogEntryType.Information, LogLevel.None, "LC-EVENT", format, args);
            LogLocally(LogLevel.None, "LC-EVENT", format, args);
        }

        [DebuggerStepThrough]
        public void LifecycleEvent(Func<string> messageGetter)
        {
            LogToEventLog(EventLogEntryType.Information, LogLevel.None, "LC-EVENT", messageGetter());
            LogLocally(LogLevel.None, "LC-EVENT", messageGetter());
        }

        [DebuggerStepThrough]
        private void LogToEventLog(EventLogEntryType entryType, LogLevel level, string logType, string format, params object[] args)
        {
            if (_eventLog == null) throw new ArgumentNullException("_eventLog");

            if (_CanLog)
            {
                if (level <= MaxLogLevel())
                {
                    _eventId++;

                    var message = ConstructLoggerMessage(logType, format, args);

                    short category = 0;

                    switch (logType)
                    {
                        case "LOG":
                            category = 1;
                            break;
                        case "WARNING":
                            category = 2;
                            break;
                        case "TRACE":
                            category = 3;
                            break;
                        case "DEBUG":
                            category = 4;
                            break;
                        case "INFO":
                            category = 5;
                            break;
                        case "ERROR":
                            category = 6;
                            break;
                        case "VERBOSE":
                            category = 7;
                            break;
                        case "LC-EVENT":
                            category = 8;
                            break;
                    }

                    _eventLog.WriteEntry(message, entryType, _eventId, category);
                }
            }
        }
    }
}