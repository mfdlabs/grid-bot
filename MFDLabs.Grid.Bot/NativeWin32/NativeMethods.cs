using System;
using System.Runtime.InteropServices;

namespace MFDLabs.Grid.Bot.NativeWin32
{
    internal sealed class NativeMethods
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();
    }
}
