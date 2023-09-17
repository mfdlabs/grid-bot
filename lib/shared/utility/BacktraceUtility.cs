namespace Grid.Bot.Utility;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Backtrace;
using Backtrace.Model;

using Logging;
using FileSystem;

/// <summary>
/// Utility for interacting with Backtrace.
/// </summary>
public static class BacktraceUtility
{
    private static readonly BacktraceClient _client;

    static BacktraceUtility()
    {
        var bckTraceCreds = new BacktraceCredentials(
            BacktraceSettings.Singleton.BacktraceUrl,
            BacktraceSettings.Singleton.BacktraceToken
        );

        _client = new BacktraceClient(bckTraceCreds);
    }

    /// <summary>
    /// Upload a crash log.
    /// </summary>
    /// <param name="ex">The <see cref="Exception"/></param>
    public static void UploadCrashLog(Exception ex)
    {
        if (string.IsNullOrEmpty(BacktraceSettings.Singleton.BacktraceUrl) ||
            string.IsNullOrEmpty(BacktraceSettings.Singleton.BacktraceToken))
            return;

        if (ex == null)
            return;

        var traceBack = $"Error: {ex}\nTrace: {Environment.StackTrace}";

        Task.Factory.StartNew(() =>
        {
            try
            {
                Console.WriteLine(Resources.BacktraceUtility_UploadCrashLog_Running);
                Console.WriteLine(traceBack);

                _client.Send(ex);

                Console.WriteLine(Resources.BacktraceUtility_UploadCrashLog_Success);
            }
            catch (Exception e)
            {
                Console.WriteLine(Resources.BacktraceUtility_UploadCrashLog_Failure, e.ToString());
            }
        });
    }

    /// <summary>
    /// Upload all log files
    /// </summary>
    /// <param name="delete">Should they be deleted?</param>
    /// <param name="uploadCurrent">Should the current log file be uploaded as well?</param>
    /// <returns>The <see cref="BacktraceResult"/></returns>
    public static BacktraceResult UploadAllLogFiles(bool delete = true, bool uploadCurrent = true)
    {
        var attachments = from file in Directory.EnumerateFiles(Path.GetDirectoryName(Logger.Singleton.FullyQualifiedFileName))
                          where file != Logger.Singleton.FullyQualifiedFileName && !uploadCurrent
                          select file;

        var result = _client.Send("Log files upload", attachmentPaths: attachments.ToList());

        if (delete)
            foreach (var log in attachments)
            {
                Logger.Singleton.Warning("Deleting old log file: {0}", log);

                log.PollDeletion();
            }

        return result;
    }
}
