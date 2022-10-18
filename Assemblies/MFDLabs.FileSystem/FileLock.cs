using System;
using System.IO;

namespace MFDLabs.FileSystem
{
    public sealed class FileLock : IDisposable
    {
        private readonly FileStream _lock;

        private bool _disposed;

        public FileLock(string path)
        {
            _lock = new(path, FileMode.Open, FileAccess.Read, FileShare.None);
        }

        public void Lock()
        {
            _lock.Lock(0, long.MaxValue);
        }

        public void Unlock()
        {
            _lock.Unlock(0, long.MaxValue);
        }

        public void Dispose()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(FileLock));

            Unlock();
            _lock?.Dispose();
            _disposed = true;
        }
    }
}
