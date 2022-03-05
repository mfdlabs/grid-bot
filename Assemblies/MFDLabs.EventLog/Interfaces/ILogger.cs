using System;

namespace MFDLabs.EventLog
{
    public interface ILogger
    {
        Func<LogLevel> MaxLogLevel { get; set; }
        bool LogThreadId { get; set; }

        void Log(string format, params object[] args);
        void Log(Func<string> messageGetter);
        void Debug(string format, params object[] args);
        void Debug(Func<string> messageGetter);
        void Error(Exception ex);
        void Error(string format, params object[] args);
        void Error(Func<string> messageGetter);
        void Info(string format, params object[] args);
        void Info(Func<string> messageGetter);
        void Warning(string format, params object[] args);
        void Warning(Func<string> messageGetter);
        void Trace(string format, params object[] args);
        void Trace(Func<string> messageGetter);
        void Verbose(string format, params object[] args);
        void Verbose(Func<string> messageGetter);
        void LifecycleEvent(string format, params object[] args);
        void LifecycleEvent(Func<string> messageGetter);
    }
}
