namespace Logging;

using System;
using System.Collections.Generic;

/// <summary>
/// Base contract for a logger class.
/// </summary>
public interface ILogger : IDisposable
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
    /// <remarks>
    /// This is only applicable when the OS supports ANSI escape codes.
    /// </remarks>
    bool LogWithColor { get; set; }

    /// <summary>
    /// Gets or sets the custom log prefixes.
    /// </summary>
    List<Func<string>> CustomLogPrefixes { get; set; }

    /// <summary>
    /// Gets the name of the log file.
    /// </summary>
    string FileName { get; }

    /// <summary>
    /// Gets the fully qualified log file name.
    /// </summary>
    string FullyQualifiedFileName { get; }

    /// <summary>
    /// Log a warning message, usually suited for messages that are not too important
    /// but are not expected behaviour.
    /// </summary>
    /// <param name="format">The message format</param>
    /// <param name="args">Optional arguments.</param>
    void Warning(string format, params object[] args);

    /// <summary>
    /// Log a warning message, usually suited for messages that are not too important
    /// but are not expected behaviour.
    /// </summary>
    /// <param name="messageGetter">A function that returns a message</param>
    void Warning(Func<string> messageGetter);

    /// <summary>
    /// Logs a verbose message, suited for extremely verbose logging, very
    /// expensive logging, and even very spammy logging, usually this should be 
    /// disabled if you wish to reduce resource usage on the app.
    /// ignore
    /// </summary>
    /// <param name="format">The message format</param>
    /// <param name="args">Optional arguments.</param>
    void Verbose(string format, params object[] args);

    /// <summary>
    /// Logs a verbose message, suited for extremely verbose logging, very
    /// expensive logging, and even very spammy logging, usually this should be 
    /// disabled if you wish to reduce resource usage on the app.
    /// </summary>
    /// <param name="messageGetter">A function that returns a message</param>
    void Verbose(Func<string> messageGetter);

    /// <summary>
    /// Logs a debug message, typically verbose information that
    /// reveals workings behind the code. This differs from <see cref="LogLevel.Verbose"/>
    /// in the way that it is for more basic verbose logging, stuff that
    /// can assist in debugging.
    /// </summary>
    /// <param name="format">The message format</param>
    /// <param name="args">Optional arguments.</param>
    void Debug(string format, params object[] args);

    /// <summary>
    /// Logs a debug message, typically verbose information that
    /// reveals workings behind the code. This differs from <see cref="LogLevel.Verbose"/>
    /// in the way that it is for more basic verbose logging, stuff that
    /// can assist in debugging.
    /// </summary>
    /// <param name="messageGetter">A function that returns a message</param>
    void Debug(Func<string> messageGetter);

    /// <summary>
    /// Logs an informational message, usually used for logging information
    /// such as starting points, life cycle events and general basic information.
    /// </summary>
    /// <param name="format">The message format</param>
    /// <param name="args">Optional arguments.</param>
    void Information(string format, params object[] args);

    /// <summary>
    /// Logs an informational message, usually used for logging information
    /// such as starting points, life cycle events and general basic information.
    /// </summary>
    /// <param name="messageGetter">A function that returns a message</param>
    void Information(Func<string> messageGetter);

    /// <summary>
    /// Log an error message, this should be used for critical
    /// errors that should be seen.
    /// </summary>
    /// <param name="ex">An exception to format.</param>
    void Error(Exception ex);

    /// <summary>
    /// Log an error message, this should be used for critical
    /// errors that should be seen.
    /// </summary>
    /// <param name="ex">An exception to format.</param>
    /// <param name="message">The message format</param>
    void Error(Exception ex, string message);

    /// <summary>
    /// Log an error message, this should be used for critical
    /// errors that should be seen.
    /// </summary>
    /// <param name="format">The message format</param>
    /// <param name="args">Optional arguments.</param>
    void Error(string format, params object[] args);

    /// <summary>
    /// Log an error message, this should be used for critical
    /// errors that should be seen.
    /// </summary>
    /// <param name="messageGetter">A function that returns a message</param>
    void Error(Func<string> messageGetter);
}