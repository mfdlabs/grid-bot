using System;
using System.Diagnostics;

namespace MFDLabs.Logging
{
    [DebuggerStepThrough]
    public static class ConsoleGlobal
    {
        [DebuggerStepThrough]
        public static void WriteContentStr(string content) => WriteContentStr(ConsoleColor.DarkGray, content);

        [DebuggerStepThrough]
        public static void WriteContentStr(ConsoleColor color, string content)
        {
            Console.Write("[");
            WriteColoredContent(color, content);
            Console.Write("]");
        }

        [DebuggerStepThrough]
        public static void WriteColoredContent(ConsoleColor color, string content)
        {
            Console.ForegroundColor = color;
            Console.Write(content);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
