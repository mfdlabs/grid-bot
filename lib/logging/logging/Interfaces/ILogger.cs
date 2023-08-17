using System;

namespace Logging
{
    /// <summary>
    /// Base contract for a logger class.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Gets or sets the name of the logger.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets the log level of the logger.
        /// </summary>
        LogLevel LogLevel { get; set; }

        /// <summary>
        /// Gets or sets the value that determines if this logger should log to a file.
        /// </summary>
        /// <remarks>When enabling this will create a log file, when disabling it will delete the log file.</remarks>
        bool LogToFileSystem { get; set; }

        /// <summary>
        /// Gets or sets the value that determines if this logger should log to the console.
        /// </summary>
        bool LogToConsole { get; set; }

        /// <summary>
        /// Gets the value that determines if the logger prefix should be shortened.
        /// </summary>
        bool CutLogPrefix { get; }

        /// <summary>
        /// Gets or sets the value that determines if the logger should log thread ids.
        /// </summary>
        bool LogThreadId { get; set; }

        /// <summary>
        /// Gets or sets the value that determines if the logger should log with color to the console.
        /// </summary>
        bool LogWithColor { get; set; }

        /// <summary>
        /// Gets the name of the log file.
        /// </summary>
        string FileName { get; }

        /// <summary>
        /// Gets the fully qualified log file name.
        /// </summary>
        string FullyQualifiedFileName { get; }

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
        /// Log an information message.
        /// </summary>
        /// <param name="format">The message format</param>
        /// <param name="args">Optional arguments.</param>
        void Information(string format, params object[] args);

        /// <summary>
        /// Log an information message.
        /// </summary>
        /// <param name="messageGetter">A function that returns a message</param>
        void Information(Func<string> messageGetter);

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
    }
}
