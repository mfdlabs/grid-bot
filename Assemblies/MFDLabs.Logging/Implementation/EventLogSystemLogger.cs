﻿#if NETFRAMEWORK

using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using MFDLabs.Text;
using MFDLabs.EventLog;
using MFDLabs.Threading;
using MFDLabs.FileSystem;
using MFDLabs.Networking;
using MFDLabs.Diagnostics;
using MFDLabs.Text.Extensions;
using MFDLabs.Logging.Diagnostics;
using MFDLabs.ErrorHandling.Extensions;

#if CONCURRENT_LOGGING_ENABLED
using Microsoft.Ccr.Core;
#endif

namespace MFDLabs.Logging
{
    [DebuggerDisplay("Global EventLog Logger")]
    [DebuggerStepThrough]
    public sealed class EventLogSystemLogger : ILogger
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public static readonly EventLogSystemLogger Singleton = new();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly string LocalIp = NetworkingGlobal.GetLocalIp();
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly string MachineId = SystemGlobal.GetMachineId();
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly string MachineHost = SystemGlobal.GetMachineHost();

#if CONCURRENT_LOGGING_ENABLED
        // Concurrent Section
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly Port<Action> MessageQueue;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly DispatcherQueue DispatcherQueue;

        [DebuggerStepThrough]
        static EventLogSystemLogger()
        {
            MessageQueue = new();
            DispatcherQueue = new PatchedDispatcherQueue("EventLog System Logger Message Queue", new(0, "EventLog System Logger Message Queue Dispatcher"));
            AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;
            Arbiter.Activate(DispatcherQueue, Arbiter.Receive(true, MessageQueue, a => a()));
        }

        [DebuggerStepThrough]
        private static void CurrentDomain_DomainUnload(object sender, EventArgs e)
        {
            DispatcherQueue?.Dispose();
        }
#endif

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly string FileBasePath = Path.Combine(@"C:\", "MFDLABS", "Logs");

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public static readonly string FileName =
#if DEBUG
            "dev_" +
#endif
            $"{SystemGlobal.AssemblyVersion}_{DateTimeGlobal.GetFileSafeUtcNowAsIso()}_{SystemGlobal.CurrentProcess.Id:X}_last.log";

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private FileSystemHelper.LockedFileStream _lockedFileStream;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static bool _canLog = true;


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private System.Diagnostics.EventLog _eventLog;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Atomic _eventId;

        [DebuggerHidden]
        [DebuggerStepThrough]
        public void Initialize(System.Diagnostics.EventLog eventLog) => _eventLog = eventLog;
        [DebuggerHidden]
        [DebuggerStepThrough]
        public bool HasEventLog() => _eventLog != null;

        public Func<LogLevel> MaxLogLevel { [DebuggerStepThrough]get; [DebuggerStepThrough]set; } = () => global::MFDLabs.Logging.Properties.Settings.Default.MaxLogLevel;

        [DebuggerHidden]
        public bool LogThreadId { get; set; } = false;

        [DebuggerStepThrough]
        private string ConstructLoggerMessage(string logType, string format, params object[] args)
        {
            var threadId = Thread.CurrentThread.ManagedThreadId.ToString("x");
            var countNCharsToReplace = 4 - threadId.Length;

            var internalMessage =
                $"[{DateTimeGlobal.GetUtcNowAsIso()}]" +
                $"[{SystemGlobal.CurrentProcess.Id:x}]" +
                $"[{threadId.Fill('0', countNCharsToReplace, TextGlobal.StringDirection.Left)}]" +
                $"[{LoggingSystem.GlobalLifetimeWatch.Elapsed.TotalSeconds:f7}]" +
                $"[{SystemGlobal.CurrentPlatform}-{SystemGlobal.CurrentDeviceArch.ToLower()}]" +
                $"[{SystemGlobal.Version}][{SystemGlobal.AssemblyVersion}]" +
#if DEBUG
                "[Debug]" +
#else
                "[Release]" +
#endif
                $"[{LocalIp}]" +
                $"[{MachineId}]" +
                $"[{MachineHost}]" +
                $"[{(global::MFDLabs.Logging.Properties.Settings.Default.LoggingUtilDataName)}]" +
                $"[{logType.ToUpper()}] {format}\n";

            return args is { Length: > 0 } ? string.Format(internalMessage, args) : internalMessage;
        }

        [DebuggerStepThrough]
        private void LogLocally(LogLevel level, string logType, string format, params object[] args)
        {
            if (!_canLog) return;
            if (level > MaxLogLevel()) return;

            _lockedFileStream ??= new(Path.Combine(FileBasePath, FileName));

            var str = ConstructLoggerMessage(logType, format, args);

            try { _lockedFileStream.AppendText(str); }
            catch
            {
                // ignored
            }
        }

        [DebuggerStepThrough]
        private void QueueLog(EventLogEntryType et, LogLevel ll, string logType, string format, params object[] args)
        {
#if CONCURRENT_LOGGING_ENABLED

            MessageQueue.Post(() =>
            {
                try
                {
                    LogToEventLog(et, ll, logType, format, args);
                    LogLocally(ll, logType, format, args);
                }
                catch { }
            });
#else
            LogToEventLog(et, ll, logType, format, args);
            LogLocally(ll, logType, format, args);
#endif
        }

        [DebuggerStepThrough]
        public void TryClearLocalLog(bool overrideEnv = false, bool wasForGlobalEventLifeTimeClosure = false)
        {
            if (!_canLog) return;

            Log("Try clear local logs...");

            if (global::MFDLabs.Logging.Properties.Settings.Default.PersistLocalLogs)
            {
                if (overrideEnv)
                {
                    Warning("Overriding global config when clearing logs.");
                }
                else
                {
                    Warning("The local log is set to persist. Please change Setting \"PersistLocalLogs\" to change this.");
                    _canLog = wasForGlobalEventLifeTimeClosure != true;
                    return;
                }
            }

            Log("Clearing LocalLog...");

            _lockedFileStream.Dispose();
            _lockedFileStream = null;

            _canLog = wasForGlobalEventLifeTimeClosure != true;

            if (!Directory.Exists(FileBasePath)) return;
            Directory.Delete(FileBasePath, true);
            Directory.CreateDirectory(FileBasePath);
        }

        [DebuggerStepThrough]
        public void Log(string format, params object[] args) => QueueLog(EventLogEntryType.Information, LogLevel.None, "LOG", format, args);
        [DebuggerStepThrough]
        public void Log(Func<string> messageGetter) => QueueLog(EventLogEntryType.Information, LogLevel.None, "LOG", messageGetter());
        [DebuggerStepThrough]
        public void Warning(string format, params object[] args) => QueueLog(EventLogEntryType.Warning, LogLevel.Warning, "WARN", format, args);
        [DebuggerStepThrough]
        public void Warning(Func<string> messageGetter) => QueueLog(EventLogEntryType.Warning, LogLevel.Warning, "Warn", messageGetter());
        [DebuggerStepThrough]
        public void Trace(string format, params object[] args) => QueueLog(EventLogEntryType.Error, LogLevel.Error, "TRACE", new Exception(format).ToDetailedString(), args);
        [DebuggerStepThrough]
        public void Trace(Func<string> messageGetter) => QueueLog(EventLogEntryType.Error, LogLevel.Error, "TRACE", new Exception(messageGetter()).ToDetailedString());
        [DebuggerStepThrough]
        public void Debug(string format, params object[] args) => QueueLog(EventLogEntryType.Warning, LogLevel.Verbose, "DEBUG", format, args);
        [DebuggerStepThrough]
        public void Debug(Func<string> messageGetter) => QueueLog(EventLogEntryType.Warning, LogLevel.Verbose, "DEBUG", messageGetter());
        [DebuggerStepThrough]
        public void Info(string format, params object[] args) => QueueLog(EventLogEntryType.Information, LogLevel.Information, "INFO", format, args);
        [DebuggerStepThrough]
        public void Info(Func<string> messageGetter) => QueueLog(EventLogEntryType.Information, LogLevel.Information, "INFO", messageGetter());
        [DebuggerStepThrough]
        public void Error(string format, params object[] args) => QueueLog(EventLogEntryType.Error, LogLevel.Error, "ERROR", format, args);
        [DebuggerStepThrough]
        public void Error(Exception ex) => QueueLog(EventLogEntryType.Error, LogLevel.Error, "ERROR", ex.ToDetailedString());
        [DebuggerStepThrough]
        public void Error(Func<string> messageGetter) => QueueLog(EventLogEntryType.Error, LogLevel.Error, "ERROR", messageGetter());
        [DebuggerStepThrough]
        public void Verbose(string format, params object[] args) => QueueLog(EventLogEntryType.Warning, LogLevel.Verbose, "VERBOSE", format, args);
        [DebuggerStepThrough]
        public void Verbose(Func<string> messageGetter) => QueueLog(EventLogEntryType.Warning, LogLevel.Verbose, "VERBOSE", messageGetter());
        [DebuggerStepThrough]
        public void LifecycleEvent(string format, params object[] args) => QueueLog(EventLogEntryType.Information, LogLevel.None, "LC-EVENT", format, args);
        [DebuggerStepThrough]
        public void LifecycleEvent(Func<string> messageGetter) => QueueLog(EventLogEntryType.Information, LogLevel.None, "LC-EVENT", messageGetter());

        [DebuggerStepThrough]
        private void LogToEventLog(EventLogEntryType entryType, LogLevel level, string logType, string format, params object[] args)
        {
            if (_eventLog == null)
                throw new ArgumentNullException(nameof(_eventLog));

            if (!_canLog) return;
            if (level > MaxLogLevel()) return;

            _eventId++;

            var message = ConstructLoggerMessage(logType, format, args);

            var category = GetEventCategory(logType);

            _eventLog.WriteEntry(message, entryType, _eventId, category);
        }

        private static short GetEventCategory(string logType)
        {
            return logType switch
            {
                "LOG" => 1,
                "WARNING" => 2,
                "TRACE" => 3,
                "DEBUG" => 4,
                "INFO" => 5,
                "ERROR" => 6,
                "VERBOSE" => 7,
                "LC-EVENT" => 8,
                _ => 0
            };
        }
    }
}

#endif