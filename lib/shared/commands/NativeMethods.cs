#if NETFRAMEWORK

using System;
using System.Runtime.InteropServices;

using HWND = System.IntPtr;
using BOOL = System.Boolean;

namespace Grid.Bot.NativeWin32
{
    internal static class NativeMethods
    {
        [DllImport("kernel32.dll")]
        public static extern HWND GetConsoleWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern BOOL ShowWindow([In] HWND hWnd, [In] int nCmdShow);
    }
}

#endif