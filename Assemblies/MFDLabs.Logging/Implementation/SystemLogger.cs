using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using MFDLabs.Abstractions;
using MFDLabs.Diagnostics;
using MFDLabs.ErrorHandling.Extensions;
using MFDLabs.EventLog;
using MFDLabs.FileSystem;
using MFDLabs.Logging.Diagnostics;
using MFDLabs.Networking;
using MFDLabs.Text;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Logging
{
    [DebuggerDisplay("Global Logger")]
    [DebuggerStepThrough]
    public sealed class SystemLogger : SingletonBase<SystemLogger>, ILogger
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly string LocalIp = NetworkingGlobal.GetLocalIp();
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly string MachineId = SystemGlobal.GetMachineId();
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly string MachineHost = SystemGlobal.GetMachineHost();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly string FileBasePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MFDLABS", "Logs");
        
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly string FileName =
#if DEBUG
                        "dev_" +
#endif
                        $"{SystemGlobal.AssemblyVersion}_{DateTimeGlobal.GetFileSafeUtcNowAsIso()}_{SystemGlobal.CurrentProcess.Id:X}_last.log";
        
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private FileSystemHelper.LockedFileStream _lockedFileStream;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly object LogSync = new();
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static bool _canLog = true;


        public Func<LogLevel> MaxLogLevel { [DebuggerStepThrough]get; [DebuggerStepThrough]set; } = () => global::MFDLabs.Logging.Properties.Settings.Default.MaxLogLevel;

        [DebuggerHidden] public bool LogThreadId { get; set; } = false;

        [DebuggerStepThrough]
        private static string ConstructLoggerMessageForLocalLogCache(string logType, string format, params object[] args)
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

            return args is {Length: > 0} ? string.Format(internalMessage, args) : internalMessage;
        }

        [DebuggerStepThrough]
        private void LogLocally(LogLevel level, string logType, string format, params object[] args)
        {
            if (!_canLog) return;
            if (level > MaxLogLevel()) return;
            
            _lockedFileStream ??= new(Path.Combine(FileBasePath, FileName));
            
            var str = ConstructLoggerMessageForLocalLogCache(logType, format, args);

            try { _lockedFileStream.AppendText(str); }
            catch
            {
                // ignored
            }
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
        public void Log(string format, params object[] args)
        {
            LogColorString(ConsoleColor.White, LogLevel.None, "LOG", format, args);
            LogLocally(LogLevel.None, "LOG", format, args);
        }

        [DebuggerStepThrough]
        public void Log(Func<string> messageGetter)
        {
            LogColorString(ConsoleColor.White, LogLevel.None, "LOG", messageGetter());
            LogLocally(LogLevel.None, "LOG", messageGetter());
        }

        [DebuggerStepThrough]
        public void Warning(string format, params object[] args)
        {
            LogColorString(ConsoleColor.Yellow, LogLevel.Warning, "Warn", format, args);
            LogLocally(LogLevel.Warning, "WARNING", format, args);
        }

        [DebuggerStepThrough]
        public void Warning(Func<string> messageGetter)
        {
            LogColorString(ConsoleColor.Yellow, LogLevel.Warning, "Warn", messageGetter());
            LogLocally(LogLevel.Warning, "WARNING", messageGetter());
        }

        [DebuggerStepThrough]
        public void Trace(string format, params object[] args)
        {
            LogColorString(ConsoleColor.Red, LogLevel.Error, "TRACE", format, args);
            LogLocally(LogLevel.Error, "TRACE", format, args);
        }

        [DebuggerStepThrough]
        public void Trace(Func<string> messageGetter)
        {
            LogColorString(ConsoleColor.Red, LogLevel.Error, "TRACE", messageGetter());
            LogLocally(LogLevel.Error, "TRACE", messageGetter());
        }

        [DebuggerStepThrough]
        public void Debug(string format, params object[] args)
        {
            LogColorString(ConsoleColor.Magenta, LogLevel.Verbose, "DEBUG", format, args);
            LogLocally(LogLevel.Verbose, "DEBUG", format, args);
        }

        [DebuggerStepThrough]
        public void Debug(Func<string> messageGetter)
        {
            LogColorString(ConsoleColor.Magenta, LogLevel.Verbose, "DEBUG", messageGetter());
            LogLocally(LogLevel.Verbose, "DEBUG", messageGetter());
        }

        [DebuggerStepThrough]
        public void Info(string format, params object[] args)
        {
            LogColorString(ConsoleColor.Blue, LogLevel.Information, "INFO", format, args);
            LogLocally(LogLevel.Information, "INFO", format, args);
        }

        [DebuggerStepThrough]
        public void Info(Func<string> messageGetter)
        {
            LogColorString(ConsoleColor.Blue, LogLevel.Information, "INFO", messageGetter());
            LogLocally(LogLevel.Information, "INFO", messageGetter());
        }

        [DebuggerStepThrough]
        public void Error(string format, params object[] args)
        {
            LogColorString(ConsoleColor.Red, LogLevel.Error, "ERROR", format, args);
            LogLocally(LogLevel.Error, "ERROR", format, args);
        }

        [DebuggerStepThrough]
        public void Error(Exception ex)
        {
            LogColorString(ConsoleColor.Red, LogLevel.Error, "ERROR", ex.ToDetailedString());
            LogLocally(LogLevel.Error, "ERROR", ex.ToDetailedString());
        }

        [DebuggerStepThrough]
        public void Error(Func<string> messageGetter)
        {
            LogColorString(ConsoleColor.Red, LogLevel.Error, "ERROR", messageGetter());
            LogLocally(LogLevel.Error, "ERROR", messageGetter());
        }

        [DebuggerStepThrough]
        public void Verbose(string format, params object[] args)
        {
            LogColorString(ConsoleColor.Cyan, LogLevel.Verbose, "VERBOSE", format, args);
            LogLocally(LogLevel.Verbose, "VERBOSE", format, args);
        }

        [DebuggerStepThrough]
        public void Verbose(Func<string> messageGetter)
        {
            LogColorString(ConsoleColor.Cyan, LogLevel.Verbose, "VERBOSE", messageGetter());
            LogLocally(LogLevel.Verbose, "VERBOSE", messageGetter());
        }

        [DebuggerStepThrough]
        public void LifecycleEvent(string format, params object[] args)
        {
            LogColorString(ConsoleColor.Green, LogLevel.None, "LC-EVENT", format, args);
            LogLocally(LogLevel.None, "LC-EVENT", format, args);
        }

        [DebuggerStepThrough]
        public void LifecycleEvent(Func<string> messageGetter)
        {
            LogColorString(ConsoleColor.Green, LogLevel.None, "LC-EVENT", messageGetter());
            LogLocally(LogLevel.None, "LC-EVENT", messageGetter());
        }

        [DebuggerStepThrough]
        private void LogColorString(ConsoleColor color, LogLevel level, string logType, string format, params object[] args)
        {
            if (!_canLog) return;
            if (level > MaxLogLevel()) return;
            
            // A lock is required here to truly make it thread safe.
            lock (LogSync)
            {
                var threadId = Thread.CurrentThread.ManagedThreadId.ToString("x");
                var countNCharsToReplace = 4 - threadId.Length;

                ConsoleGlobal.WriteContentStr(DateTimeGlobal.GetUtcNowAsIso());
                ConsoleGlobal.WriteContentStr(LoggingSystem.GlobalLifetimeWatch.Elapsed.TotalSeconds.ToString("f7"));
                ConsoleGlobal.WriteContentStr(SystemGlobal.CurrentProcess.Id.ToString("x"));
                ConsoleGlobal.WriteContentStr(threadId.Fill('0', countNCharsToReplace, TextGlobal.StringDirection.Left));
                ConsoleGlobal.WriteContentStr($"{SystemGlobal.CurrentPlatform}-{SystemGlobal.CurrentDeviceArch.ToLower()}");
                ConsoleGlobal.WriteContentStr(SystemGlobal.Version);
                ConsoleGlobal.WriteContentStr(SystemGlobal.AssemblyVersion);
#if DEBUG
                ConsoleGlobal.WriteContentStr("Debug");
#else
                ConsoleGlobal.WriteContentStr("Release");
#endif
                ConsoleGlobal.WriteContentStr(LocalIp);
                ConsoleGlobal.WriteContentStr(MachineId);
                ConsoleGlobal.WriteContentStr(MachineHost);
                ConsoleGlobal.WriteContentStr(ConsoleColor.White, global::MFDLabs.Logging.Properties.Settings.Default.LoggingUtilDataName);
                ConsoleGlobal.WriteContentStr(color, logType.ToUpper());
                var message = args is {Length: > 0} ? string.Format($" {format}\n", args) : $" {format}\n";
                ConsoleGlobal.WriteColoredContent(color, message);
            }
        }
    }
}