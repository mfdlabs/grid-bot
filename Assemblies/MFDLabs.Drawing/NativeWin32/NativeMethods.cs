using MFDLabs.Drawing.Models;
using System;
using System.Runtime.InteropServices;

namespace MFDLabs.Drawing.NativeWin32
{
    internal class NativeMethods
    {
        [DllImport("user32.dll")]
        internal static extern bool GetWindowRect(IntPtr hWnd, out BaseRectangle lpRect);

        [DllImport("user32.dll")]
        internal static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);
    }
}
