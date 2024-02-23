namespace Grid.Bot.Utility;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
        if (backtraceSettings == null)
            throw new ArgumentNullException(nameof(backtraceSettings));
            
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (!string.IsNullOrEmpty(backtraceSettings.BacktraceUrl))
        {
            var bckTraceCreds = new BacktraceCredentials(
                backtraceSettings.BacktraceUrl,
                backtraceSettings.BacktraceToken
            );

            _client = new BacktraceClient(bckTraceCreds);
        }
    }

    /// <inheritdoc cref="IBacktraceUtility.Client"/>
    public IBacktraceClient Client => _client;

    /// <inheritdoc cref="IBacktraceUtility.UploadException(Exception)"/>
    public void UploadException(Exception ex)
    {
        if (ex == null)
            return;

        var traceBack = $"Error: {ex}\nTrace: {Environment.StackTrace}";

        Task.Factory.StartNew(() =>
        {
            try
            {
                Console.WriteLine(Resources.BacktraceUtility_UploadCrashLog_Running);
                Console.WriteLine(traceBack);

                _client?.Send(ex);

                Console.WriteLine(Resources.BacktraceUtility_UploadCrashLog_Success);
            }
            catch (Exception e)
            {
                Console.WriteLine(Resources.BacktraceUtility_UploadCrashLog_Failure, e.ToString());
            }
        });
    }

    /// <inheritdoc cref="IBacktraceUtility.UploadAllLogFiles(bool)"/>
    public BacktraceResult UploadAllLogFiles(bool delete = true)
    {
        var attachments = from file in Directory.EnumerateFiles(Path.GetDirectoryName(Logger.LogFileBaseDirectory))
                          select file;

        if (!attachments.Any())
            return null;

        var result = _client?.Send("Log files upload", attachmentPaths: attachments.ToList());

        if (delete)
            foreach (var log in attachments)
            {
                if (log == _logger.FullyQualifiedFileName) continue;
                
                _logger.Warning("Deleting old log file: {0}", log);

                log.PollDeletion();
            }

        return result;
    }
}
