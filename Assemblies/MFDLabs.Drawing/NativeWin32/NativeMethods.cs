using System;
using System.Runtime.InteropServices;
using MFDLabs.Drawing.Models;

namespace MFDLabs.Drawing.NativeWin32
{
    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        internal static extern bool GetWindowRect(IntPtr hWnd, out BaseRectangle lpRect);

        [DllImport("user32.dll")]
        internal static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);
    }
}
