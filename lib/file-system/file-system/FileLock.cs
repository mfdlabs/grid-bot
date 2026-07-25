namespace FileSystem;

using System;
using System.IO;

/// <summary>
/// Represents a lock that takes ownership over a file.
/// </summary>
public sealed class FileLock : IDisposable
{
    private readonly FileStream _lock;

    private bool _disposed;

    /// <summary>
    /// Construct a new instance of <see cref="FileLock"/>
    /// </summary>
    /// <param name="path">The path to the file.</param>
    public FileLock(string path)
    {
        _lock = new(path, FileMode.Open, FileAccess.Read, FileShare.None);
    }

    /// <summary>
    /// Lock the file.
    /// </summary>
    public void Lock()
    {
        _lock.Lock(0, long.MaxValue);
    }

    /// <summary>
    /// Unlock the file.
    /// </summary>
    public void Unlock()
    {
        _lock.Unlock(0, long.MaxValue);
    }

    /// <summary>
    /// Dipose of the lock.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The object is disposed.</exception>
    public void Dispose()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(FileLock));

        Unlock();
        _lock?.Dispose();
        _disposed = true;
    }
}
