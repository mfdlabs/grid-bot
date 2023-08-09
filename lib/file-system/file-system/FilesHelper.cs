using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileSystem
{
    public static class FilesSystemHelper
    {
        private static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(2.5);
        private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(10);

        public static bool IsFilePath(this string s) => s.IndexOfAny(Path.GetInvalidPathChars()) == -1;

        public static bool IsDirectoryWritable(this string dirPath, bool throwIfFails = false)
        {
            try
            {
                using (var fs = File.Create(
                    Path.Combine(
                        dirPath,
                        Path.GetRandomFileName()
                    ),
                    1,
                    FileOptions.DeleteOnClose)
                )
                { }
                return true;
            }
            catch
            {
                if (throwIfFails)
                    throw;
                else
                    return false;
            }
        }

        public static bool CreateFileRecursive(string filePath)
        {
            if (!filePath.IsFilePath()) return false;

            if (File.Exists(filePath)) return true;

            var directory = Path.GetDirectoryName(filePath);

            if (directory == null) return false;

            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            File.Create(filePath);

            return true;
        }

        public static bool AppendAllTextToFileRecursive(string filePath, string text, Encoding encoding = null)
        {
            encoding ??= Encoding.Default;

            if (!File.Exists(filePath))
                if (!CreateFileRecursive(filePath))
                    return false;


            File.AppendAllText(filePath, text, encoding);

            return true;
        }

        public static bool IsValidPath(this string path, bool allowRelativePaths = false)
        {
            try
            {
                var fullPath = Path.GetFullPath(path);

                if (allowRelativePaths)
                    return Path.IsPathRooted(path);

                var root = Path.GetPathRoot(path);
                return !string.IsNullOrEmpty(root.Trim(new char[] { '\\', '/' }));
            }
            catch
            {
                return false;
            }
        }

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

        public static void PollDeletion(this string path, int maxAttempts = 10, Action<Exception> onFailure = null, Action onSuccess = null)
            => Task.Factory.StartNew(() => PollDeletionBlocking(path, maxAttempts, onFailure, onSuccess));
        public static void PollDeletion(this string path, int maxAttempts, Action<Exception> onFailure, Action onSuccess, TimeSpan baseDelay, TimeSpan maxDelay, Jitter jitter = Jitter.None)
            => Task.Factory.StartNew(() => PollDeletionBlocking(path, maxAttempts, onFailure, onSuccess, baseDelay, maxDelay, jitter));
        public static void PollDeletionBlocking(this string path, int maxAttempts = 10, Action<Exception> onFailure = null, Action onSuccess = null)
            => PollDeletionBlocking(path, maxAttempts, onFailure, onSuccess, BaseDelay, MaxDelay);
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
}
