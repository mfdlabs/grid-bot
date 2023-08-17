using System;
using System.Runtime.InteropServices;
using Drawing.Models;

namespace Drawing.NativeWin32
{
    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        internal static extern bool GetWindowRect(IntPtr hWnd, out BaseRectangle lpRect);

        [DllImport("user32.dll")]
        internal static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);
    }
}
