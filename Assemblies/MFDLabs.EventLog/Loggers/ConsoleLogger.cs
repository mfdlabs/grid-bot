using System;

namespace MFDLabs.EventLog
{
    public class ConsoleLogger : LoggerBase
    {
        public ConsoleLogger(Func<LogLevel> maxLogLevel, bool logThreadId = false, bool removeLineBreaks = false)
        {
            MaxLogLevel = maxLogLevel;
            LogThreadID = logThreadId;
            _RemoveLineBreaks = removeLineBreaks;
        }

        protected override void Log(LogLevel logLevel, string format, params object[] args)
        {
            var message = (args != null && args.Length != 0) ? string.Format(format, args) : format;
            if (_RemoveLineBreaks)
            {
                message = message.Replace("\r", "").Replace("\n", ", ");
            }
            Console.WriteLine($"{DateTime.Now} - {logLevel} - {message}");
        }

        private readonly bool _RemoveLineBreaks;
    }
}
