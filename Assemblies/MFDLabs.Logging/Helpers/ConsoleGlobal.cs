using MFDLabs.Abstractions;
using System;
using System.Diagnostics;

namespace MFDLabs.Logging
{
    [DebuggerStepThrough]
    public sealed class ConsoleGlobal : SingletonBase<ConsoleGlobal>
    {
        [DebuggerStepThrough]
        public void WriteContentStr(string content)
        {
            WriteContentStr(ConsoleColor.DarkGray, content);
        }

        [DebuggerStepThrough]
        public void WriteContentStr(ConsoleColor color, string content)
        {
            Console.Write("[");
            WriteColoredContent(color, content);
            Console.Write("]");
        }

        [DebuggerStepThrough]
        public void WriteColoredContent(ConsoleColor color, string content)
        {
            Console.ForegroundColor = color;
            Console.Write(content);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
