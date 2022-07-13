using System;
using System.IO;
using System.Text;

namespace MFDLabs.FileSystem
{
    public class LockedFileStream : Stream, IDisposable
    {
        private readonly FileStream _lock;
        private readonly StreamWriter _writer;

        public LockedFileStream(string path, Encoding encoding = null)
        {
            encoding ??= Encoding.Default;
            _lock = new(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            _writer = new(_lock, encoding) { AutoFlush = true };
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _lock.Length;
        
        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void Write(string text) => _writer?.Write(text);
        public override void Write(byte[] buffer, int offset, int count) => _writer?.Write(_writer?.Encoding.GetString(buffer).ToCharArray(), offset, count);
        public override void Flush() => _writer?.Flush();

        public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
        public override void SetLength(long value) => throw new NotImplementedException();

        public new void Dispose()
        {
            GC.SuppressFinalize(this);
            _writer?.Close();
            _writer?.Dispose();
            _lock?.Close();
            _lock?.Dispose();
        }

    }
}