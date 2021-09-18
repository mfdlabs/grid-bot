using System.Runtime.InteropServices;
using DWORD = System.UInt32;
using HANDLE = System.IntPtr;
using HWND = System.IntPtr;
using PHANDLE = System.IntPtr;

namespace MFDLabs.Diagnostics.NativeWin32
{
    public class NativeMethods
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool OpenProcessToken(HANDLE hProcess, DWORD dwAccess, out PHANDLE phToken);
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(HANDLE hObject);
        [DllImport("oleacc.dll", SetLastError = true)]
        internal static extern HANDLE GetProcessHandleFromHwnd(/*_In_*/ HWND hwnd);
    }
}
