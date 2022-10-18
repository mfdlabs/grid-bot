using System;

namespace MFDLabs.Logging
{
    /// <summary>
    /// Base contract for a logger class.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Defines a method that gets the max log level.
        /// </summary>
        Func<LogLevel> MaxLogLevel { get; set; }

        /// <summary>
        /// Defines a boolean that will determine if we log the thread ID.
        /// 
        /// Not used in every logger.
        /// </summary>
        bool LogThreadId { get; set; }

        /// <summary>
        /// Log a log message.
        /// </summary>
        /// <param name="format">The message format</param>
        /// <param name="args">Optional arguments.</param>
        void Log(string format, params object[] args);
        
        /// <summary>
        /// Log a log message.
        /// </summary>
        /// <param name="messageGetter">A function that returns a message</param>
        void Log(Func<string> messageGetter);

        /// <summary>
        /// Log a debug message.
        /// </summary>
        /// <param name="format">The message format</param>
        /// <param name="args">Optional arguments.</param>
        void Debug(string format, params object[] args);

        /// <summary>
        /// Log a debug message.
        /// </summary>
        /// <param name="messageGetter">A function that returns a message</param>
        void Debug(Func<string> messageGetter);

        /// <summary>
        /// Log an error message.
        /// </summary>
        /// <param name="ex">An exception to format.</param>
        void Error(Exception ex);

        /// <summary>
        /// Log an error message.
        /// </summary>
        /// <param name="format">The message format</param>
        /// <param name="args">Optional arguments.</param>
        void Error(string format, params object[] args);

        /// <summary>
        /// Log an error message.
        /// </summary>
        /// <param name="messageGetter">A function that returns a message</param>
        void Error(Func<string> messageGetter);

        /// <summary>
        /// Log an information message.
        /// </summary>
        /// <param name="format">The message format</param>
        /// <param name="args">Optional arguments.</param>
        void Info(string format, params object[] args);

        /// <summary>
        /// Log an information message.
        /// </summary>
        /// <param name="messageGetter">A function that returns a message</param>
        void Info(Func<string> messageGetter);

        /// <summary>
        /// Log a warning message.
        /// </summary>
        /// <param name="format">The message format</param>
        /// <param name="args">Optional arguments.</param>
        void Warning(string format, params object[] args);

        /// <summary>
        /// Log a warning message.
        /// </summary>
        /// <param name="messageGetter">A function that returns a message</param>
        void Warning(Func<string> messageGetter);

        /// <summary>
        /// Log a trace message.
        /// </summary>
        /// <param name="format">The message format</param>
        /// <param name="args">Optional arguments.</param>
        void Trace(string format, params object[] args);

        /// <summary>
        /// Log a trace message.
        /// </summary>
        /// <param name="messageGetter">A function that returns a message</param>
        void Trace(Func<string> messageGetter);

        /// <summary>
        /// Log a verbose message.
        /// </summary>
        /// <param name="format">The message format</param>
        /// <param name="args">Optional arguments.</param>
        void Verbose(string format, params object[] args);

        /// <summary>
        /// Log a verbose message.
        /// </summary>
        /// <param name="messageGetter">A function that returns a message</param>
        void Verbose(Func<string> messageGetter);

        /// <summary>
        /// Log a life cycle event message.
        /// </summary>
        /// <param name="format">The message format</param>
        /// <param name="args">Optional arguments.</param>
        void LifecycleEvent(string format, params object[] args);

        /// <summary>
        /// Log a life cycle event message.
        /// </summary>
        /// <param name="messageGetter">A function that returns a message</param>
        void LifecycleEvent(Func<string> messageGetter);
    }
}
