using System;

namespace MFDLabs.Logging
{
    public class ConsoleLogger : LoggerBase
    {
        public ConsoleLogger(Func<LogLevel> maxLogLevel, bool logThreadId = false, bool removeLineBreaks = false)
        {
            MaxLogLevel = maxLogLevel;
            LogThreadId = logThreadId;
            _removeLineBreaks = removeLineBreaks;
        }

        protected override void Log(LogLevel logLevel, string format, params object[] args)
        {
            var message = (args != null && args.Length != 0) ? string.Format(format, args) : format;
            if (_removeLineBreaks) message = message.Replace("\r", "").Replace("\n", ", ");
            Console.WriteLine($"{DateTime.Now} - {logLevel} - {message}");
        }

        private readonly bool _removeLineBreaks;
    }
}
