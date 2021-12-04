﻿using System;
using System.Collections.Generic;
using System.Text;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DynamicProxyGenAssembly2")]
namespace Backtrace.Interfaces.Database
{
    internal interface IBacktraceDatabaseRecordWriter
    {
        string Write(object data, string prefix);
        string Write(byte[] data, string prefix);
        void SaveTemporaryFile(string path, byte[] file);
        string ToJsonFile(object data);
        void SaveValidRecord(string sourcePath, string destinationPath);
    }
}
