namespace Grid.Bot.Utility;

using System;

using Backtrace.Model;
using Backtrace.Interfaces;

/// <summary>
/// Utility class for Backtrace.NET
/// </summary>
public interface IBacktraceUtility
{
    /// <summary>
    /// Gets the Backtrace client.
    /// </summary>
    IBacktraceClient Client { get; }

    /// <summary>
    /// Upload an exception to Backtrace
    /// </summary>
    /// <param name="ex">The <see cref="Exception"/></param>
    void UploadException(Exception ex);

    /// <summary>
    /// Upload all log files
    /// </summary>
    /// <param name="delete">Should they be deleted?</param>
    /// <returns>The <see cref="BacktraceResult"/></returns>
    BacktraceResult UploadAllLogFiles(bool delete = true);
}
