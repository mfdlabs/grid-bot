﻿using System;
using System.IO;
using System.Threading;
using MFDLabs.Sentinels;

namespace MFDLabs.FileSystem
{
    public static class FilesHelper
    {
        private static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(2.5);
        private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(10);

        public static bool IsValidPath(string path, bool allowRelativePaths = false)
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

        public static bool IsSubDir(string parentPath, string childPath)
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

        public static void PollDeletionOfFile(string path, int maxAttempts = 10, Action<Exception> onFailure = null, Action onSuccess = null)
            => ThreadPool.QueueUserWorkItem(s => PollDeletionOfFileBlocking(path, maxAttempts, onFailure, onSuccess));
        public static void PollDeletionOfFile(string path, int maxAttempts, Action<Exception> onFailure, Action onSuccess, TimeSpan baseDelay, TimeSpan maxDelay, Jitter jitter = Jitter.None)
            => ThreadPool.QueueUserWorkItem(s => PollDeletionOfFileBlocking(path, maxAttempts, onFailure, onSuccess, baseDelay, maxDelay, jitter));
        public static void PollDeletionOfFileBlocking(string path, int maxAttempts = 10, Action<Exception> onFailure = null, Action onSuccess = null)
            => PollDeletionOfFileBlocking(path, maxAttempts, onFailure, onSuccess, BaseDelay, MaxDelay);
        public static void PollDeletionOfFileBlocking(string path, int maxAttempts, Action<Exception> onFailure, Action onSuccess, TimeSpan baseDelay, TimeSpan maxDelay, Jitter jitter = Jitter.None)
        {
            if (maxAttempts <= 0) throw new ArgumentOutOfRangeException(nameof(maxAttempts));

            for (int i = 1; i <= maxAttempts; i++)
            {
                try
                {
                    File.Delete(path);
                    onSuccess?.Invoke();
                    return;
                }
                catch (Exception ex) { onFailure?.Invoke(ex); }

                Thread.Sleep(ExponentialBackoff.CalculateBackoff((byte)i, (byte)maxAttempts, baseDelay, maxDelay, jitter));
            }

            onFailure?.Invoke(new TimeoutException($"Unable to delete the file '{path}' within the max attempts of {maxAttempts}"));
        }
    }
}
