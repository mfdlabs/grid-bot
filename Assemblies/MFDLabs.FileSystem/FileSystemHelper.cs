using System;
using System.IO;
using System.Text;

namespace MFDLabs.FileSystem
{
    public static class FileSystemHelper
    {
        public class LockedFileStream : IDisposable
        {
            private readonly FileStream _lock;
            private readonly StreamWriter _writer;
            
            public LockedFileStream(string path, Encoding encoding = null)
            {
                encoding ??= Encoding.Default;
                _lock = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                _writer = new(_lock, encoding);
            }

            public void AppendText(string text) => _writer.Write(text);

            public void Dispose()
            {
                GC.SuppressFinalize(this);
                _writer?.Close();
                _writer?.Dispose();
                _lock?.Close();
                _lock?.Dispose();
            }
        }
        
        public static bool IsFilePath(this string s) => s.IndexOfAny(Path.GetInvalidPathChars()) == -1;
        
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
    }
}