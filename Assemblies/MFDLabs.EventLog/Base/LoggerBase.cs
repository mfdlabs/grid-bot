using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using MFDLabs.Text.Extensions;

namespace MFDLabs.EventLog
{
    public abstract class LoggerBase : ILogger
    {
        public Func<LogLevel> MaxLogLevel { get; set; }
        public bool LogMethodName { get; set; }
        public bool LogClassAndMethodName { get; set; }
        public bool LogThreadID { get; set; }
        public Func<string> EscalationSearchString { get; set; }
        public Func<LogLevel> EscalationLogLevel { get; set; }
        public bool IsDefaultLog
        {
            set
            {
                if (value) StaticLoggerRegistry.SetLogger(this);
            }
        }

        protected virtual string GetPrefix(LogLevel logLevel)
        {
            var builder = new StringBuilder();
            if (LogThreadID)
            {
                var thisThread = Thread.CurrentThread;
                string arg = thisThread.Name ?? thisThread.ManagedThreadId.ToString();
                builder.AppendFormat("[{0}] ", arg);
            }
            if (LogMethodName || LogClassAndMethodName)
            {
                var method = new StackFrame(3, true).GetMethod();
                var fullyQualifiedMethodName = "";
                if (LogClassAndMethodName) 
                    fullyQualifiedMethodName = (method != null && method.DeclaringType != null) ? (method.DeclaringType.Name + ".") : "";
                builder.AppendFormat("[{0}{1}] ", fullyQualifiedMethodName, method?.Name ?? "<Unknown method>");
            }
            return builder.ToString();
        }
        protected abstract void Log(LogLevel logLevel, string format, params object[] args);
        public void Debug(string format, params object[] args) { }
        public void Debug(Func<string> messageGetter) { }
        public void Error(Exception ex) => LogIfNeededLazy(LogLevel.Error, ex.ToString);
        public void Error(string format, params object[] args) => LogIfNeeded(LogLevel.Error, format, args);
        public void Error(Func<string> messageGetter) => LogIfNeededLazy(LogLevel.Error, messageGetter);
        public void Info(string format, params object[] args) => LogIfNeeded(LogLevel.Information, format, args);
        public void Info(Func<string> messageGetter) => LogIfNeededLazy(LogLevel.Information, messageGetter);
        public void Warning(string format, params object[] args) => LogIfNeeded(LogLevel.Warning, format, args);
        public void Warning(Func<string> messageGetter) => LogIfNeededLazy(LogLevel.Warning, messageGetter);
        public void Verbose(string format, params object[] args) => LogIfNeeded(LogLevel.Verbose, format, args);
        public void Verbose(Func<string> messageGetter) => LogIfNeededLazy(LogLevel.Verbose, messageGetter);
        public void LifecycleEvent(string format, params object[] args) => Log(LogLevel.Information, format, args);
        public void LifecycleEvent(Func<string> messageGetter) => Log(LogLevel.Information, messageGetter(), null);
        internal void LogIfNeeded(LogLevel level, string message, object[] args = null)
        {
            if (level == LogLevel.Error)
            {
                LogWithDiagnosticsIfNecessary(level, message, args);
                return;
            }
            var maxLogLevel = MaxLogLevel();
            var search = EscalationSearchString?.Invoke();
            if (EscalationLogLevel != null && EscalationLogLevel() < level && !search.IsNullOrEmpty() && message.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                level = (maxLogLevel < EscalationLogLevel()) ? maxLogLevel : EscalationLogLevel();
            if (maxLogLevel >= level) LogWithDiagnosticsIfNecessary(level, message, args);
        }
        internal void LogIfNeededLazy(LogLevel level, Func<string> messageGetter)
        {
            if (level == LogLevel.Error)
            {
                LogWithDiagnosticsIfNecessary(level, messageGetter());
                return;
            }
            var maxLogLevel = MaxLogLevel();
            string search = EscalationSearchString?.Invoke();
            string message = null;
            if (EscalationLogLevel != null && EscalationLogLevel() < level && !search.IsNullOrEmpty())
            {
                message = messageGetter();
                if (message.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                    level = (maxLogLevel < EscalationLogLevel()) ? maxLogLevel : EscalationLogLevel();
            }
            if (maxLogLevel >= level)
            {
                message = message ?? messageGetter();
                LogWithDiagnosticsIfNecessary(level, message);
            }
        }
        private void LogWithDiagnosticsIfNecessary(LogLevel level, string message, params object[] args) => Log(level, message, args);
        public static void LogSafely(Action logLineAction) { try { logLineAction(); } catch { } }
    }
}
