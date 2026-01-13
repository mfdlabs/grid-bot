namespace Grid.Bot.Utility;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Prometheus;

using Backtrace;
using Backtrace.Model;
using Backtrace.Interfaces;

using Logging;
using FileSystem;

/// <summary>
/// Utility for interacting with Backtrace.
/// </summary>
public class BacktraceUtility : IBacktraceUtility
{
    private readonly ILogger _logger;
    private readonly BacktraceClient _client;

    private static readonly Counter UploadExceptionCounter = Metrics.CreateCounter(
        "backtrace_exception_upload_total",
        "Total number of exceptions uploaded to Backtrace"
    );

    /// <summary>
    /// Construct a new instance of <see cref="BacktraceUtility"/>.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="backtraceSettings">The <see cref="BacktraceSettings"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="backtraceSettings"/> cannot be null.
    /// - <paramref name="logger"/> cannot be null.
    /// </exception>
    public BacktraceUtility(ILogger logger, BacktraceSettings backtraceSettings)
    {
        ArgumentNullException.ThrowIfNull(backtraceSettings);

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrEmpty(backtraceSettings.BacktraceUrl)) return;
        var bckTraceCreds = new BacktraceCredentials(
            backtraceSettings.BacktraceUrl,
            backtraceSettings.BacktraceToken
        );

        _client = new BacktraceClient(bckTraceCreds);
    }

    /// <inheritdoc cref="IBacktraceUtility.Client"/>
    public IBacktraceClient Client => _client;

    /// <inheritdoc cref="IBacktraceUtility.UploadException(Exception)"/>
    public void UploadException(Exception ex)
    {
        if (ex == null)
            return;

        UploadExceptionCounter.Inc();

        Task.Factory.StartNew(() =>
        {
            try
            {
                Console.WriteLine("Uploading exception to Backtrace...");

                _client?.Send(ex);

                Console.WriteLine("Exception uploaded to Backtrace.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to upload exception to Backtrace: {0}", e);
            }
        });
    }

    private static bool IsFileLocked(string filePath)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None);

            stream.Close();
        }
        catch (IOException)
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc cref="IBacktraceUtility.UploadAllLogFiles(bool)"/>
    public BacktraceResult UploadAllLogFiles(bool delete = true)
    {
        var attachments = (from file in Directory.EnumerateFiles(Logger.LogFileBaseDirectory)
            // Do not include files that are in use.
            where File.Exists(file) && !IsFileLocked(file)
            select file).ToArray();

        if (attachments.Length == 0)
        {
            _logger.Warning("No log files found to upload!");
        
            return null;
        }

        _logger.Information("Uploading the following log files to Backtrace: {0}", string.Join(", ", from log in attachments select Path.GetFileName(log)));

        var result = _client?.Send("Log files upload", attachmentPaths: [.. attachments]);

        if (!delete) return result;
        
        foreach (var log in attachments)
        {
            if (log == _logger.FullyQualifiedFileName) continue;
                
            _logger.Warning("Deleting old log file: {0}", log);

            log.PollDeletion();
        }

        return result;
    }
}
