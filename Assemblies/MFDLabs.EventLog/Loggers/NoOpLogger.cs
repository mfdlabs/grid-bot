﻿using System;

namespace MFDLabs.EventLog
{
    public class NoOpLogger : ILogger
    {
        public Func<LogLevel> MaxLogLevel { get; set; }
        public bool LogThreadId { get; set; }
        public bool IsDefaultLog
        {
            set
            {
                if (value) StaticLoggerRegistry.SetLogger(this);
            }
        }

        public void Log(string format, params object[] args) { }
        public void Log(Func<string> messageGetter) { }
        public void Debug(string format, params object[] args) { }
        public void Debug(Func<string> messageGetter) { }
        public void Error(Exception ex) { }
        public void Error(string format, params object[] args) { }
        public void Error(Func<string> messageGetter) { }
        public void Info(string format, params object[] args) { }
        public void Info(Func<string> messageGetter) { }
        public void Warning(string format, params object[] args) { }
        public void Warning(Func<string> messageGetter) { }
        public void Trace(string format, params object[] args) { }
        public void Trace(Func<string> messageGetter) { }
        public void Verbose(string format, params object[] args) { }
        public void Verbose(Func<string> messageGetter) { }
        public void LifecycleEvent(string format, params object[] args) { }
        public void LifecycleEvent(Func<string> messageGetter) { }

        public static readonly NoOpLogger Instance = new();
    }
}
