namespace FileSystem;

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Helper class with extension methods to interact with file systems.
/// </summary>
public static class FilesSystemHelper
{
    private static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(2.5);
    private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Is the specified path a valid file system path?
    /// </summary>
    /// <param name="s">The string.</param>
    /// <returns>True if the string is a valid file system path.</returns>
    public static bool IsFilePath(this string s) => s.IndexOfAny(Path.GetInvalidPathChars()) == -1;

    /// <summary>
    /// Is the specified directory writable?
    /// </summary>
    /// <param name="dirPath">The path of the directory.</param>
    /// <returns>True if the directory is writable.</returns>
    public static bool IsDirectoryWritable(this string dirPath)
    {
        try
        {
            using var _ = File.Create(
                Path.Combine(
                    dirPath,
                    Path.GetRandomFileName()
                ),
                1,
                FileOptions.DeleteOnClose
            );

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Is the specified path a sub dir of the parent?
    /// </summary>
    /// <param name="parentPath">The parent path.</param>
    /// <param name="childPath">The child path.</param>
    /// <returns>True if the path is child of the parent.</returns>
    public static bool IsSubDir(this string parentPath, string childPath)
    {
        var parentUri = new Uri(parentPath);
        var childUri = new DirectoryInfo(childPath).Parent;

        while (childUri != null)
        {
            if (new Uri(childUri.FullName) == parentUri)
                return true;

            childUri = childUri.Parent;
        }

        return false;
    }

    /// <summary>
    /// Polls the deletion of a file if it fails.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="maxAttempts">Max attempts.</param>
    /// <param name="onFailure">Function to invoke when failure.</param>
    /// <param name="onSuccess">Function to invoke when success.</param>
    public static void PollDeletion(this string path, int maxAttempts = 10, Action<Exception> onFailure = null, Action onSuccess = null)
        => Task.Factory.StartNew(() => PollDeletionBlocking(path, maxAttempts, onFailure, onSuccess));

    /// <summary>
    /// Polls the deletion of a file if it fails.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="maxAttempts">Max attempts.</param>
    /// <param name="onFailure">Function to invoke when failure.</param>
    /// <param name="onSuccess">Function to invoke when success.</param>
    /// <param name="baseDelay">The base delay.</param>
    /// <param name="maxDelay">The max delay.</param>
    /// <param name="jitter">The <see cref="Jitter"/></param>
    public static void PollDeletion(this string path, int maxAttempts, Action<Exception> onFailure, Action onSuccess, TimeSpan baseDelay, TimeSpan maxDelay, Jitter jitter = Jitter.None)
        => Task.Factory.StartNew(() => PollDeletionBlocking(path, maxAttempts, onFailure, onSuccess, baseDelay, maxDelay, jitter));


    /// <summary>
    /// Polls the deletion of a file if it fails.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="maxAttempts">Max attempts.</param>
    /// <param name="onFailure">Function to invoke when failure.</param>
    /// <param name="onSuccess">Function to invoke when success.</param>
    public static void PollDeletionBlocking(this string path, int maxAttempts = 10, Action<Exception> onFailure = null, Action onSuccess = null)
        => PollDeletionBlocking(path, maxAttempts, onFailure, onSuccess, BaseDelay, MaxDelay);

    /// <summary>
    /// Polls the deletion of a file if it fails.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="maxAttempts">Max attempts.</param>
    /// <param name="onFailure">Function to invoke when failure.</param>
    /// <param name="onSuccess">Function to invoke when success.</param>
    /// <param name="baseDelay">The base delay.</param>
    /// <param name="maxDelay">The max delay.</param>
    /// <param name="jitter">The <see cref="Jitter"/></param>
    public static void PollDeletionBlocking(this string path, int maxAttempts, Action<Exception> onFailure, Action onSuccess, TimeSpan baseDelay, TimeSpan maxDelay, Jitter jitter = Jitter.None)
    {
        if (maxAttempts <= 0) throw new ArgumentOutOfRangeException(nameof(maxAttempts));
        if (maxAttempts > 255) throw new ArgumentOutOfRangeException(nameof(maxAttempts));

        var maximumAttempts = (byte)maxAttempts;

        for (byte i = 1; i <= maximumAttempts; i++)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
                else if (Directory.Exists(path))
                    Directory.Delete(path, true);

                onSuccess?.Invoke();

                return;
            }
            catch (Exception ex) { onFailure?.Invoke(ex); }

            Thread.Sleep(ExponentialBackoff.CalculateBackoff(i, maximumAttempts, baseDelay, maxDelay, jitter));
        }

        onFailure?.Invoke(new TimeoutException($"Unable to delete the file '{path}' within the max attempts of {maxAttempts}"));
    }
}
