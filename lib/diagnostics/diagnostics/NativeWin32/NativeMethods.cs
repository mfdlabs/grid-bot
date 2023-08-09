#if NETFRAMEWORK

using System.Runtime.InteropServices;

using HANDLE = System.IntPtr;
using HWND = System.IntPtr;

namespace MFDLabs.Diagnostics.NativeWin32
{
    public static class Oleacc
    {
        [DllImport("oleacc.dll", SetLastError = true)]
        internal static extern HANDLE GetProcessHandleFromHwnd([In] HWND hwnd);
    }
}

#endif