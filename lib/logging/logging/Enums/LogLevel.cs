namespace Logging;

/// <summary>
/// Represents the logging level.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// NoOp log, no log messages are ever written.
    /// </summary>
    None,

    /// <summary>
    /// Only critical error messages are written.
    /// </summary>
    Error,

    /// <summary>
    /// Error messages as well as warning type messages are written.
    /// </summary>
    Warning,

    /// <summary>
    /// More chatty information based messages are written as well.
    /// </summary>
    Information,

    /// <summary>
    /// Verbose logging information is written.
    /// </summary>
    Debug,

    /// <summary>
    /// Extremely verbose and possibly spammy logging messages are written.
    /// </summary>
    Verbose
}