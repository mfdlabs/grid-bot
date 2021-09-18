using MFDLabs.Diagnostics.Extensions;
using MFDLabs.Diagnostics.NativeWin32;
using MFDLabs.Text.Extensions;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;
using HANDLE = System.IntPtr;
using HWND = System.IntPtr;
using PHANDLE = System.IntPtr;

namespace MFDLabs.Diagnostics
{
    public class ProcessHelper
    {
        public static bool GetProcessByWindowTitle(string windowTitle, out Process pr)
        {
            var processes = Process.GetProcesses();

            foreach (var p in processes)
            {
                if (!p.MainWindowTitle.IsNullOrEmpty())
                {
                    if (p.MainWindowTitle == windowTitle)
                    {
                        pr = p;
                        return true;
                    }
                }
            }

            pr = null;

            return false;
        }

        public static bool ProcessIsElevatedByWindowHandle(HWND hwnd)
        {
            try
            {
                var hProcess = NativeMethods.GetProcessHandleFromHwnd(hwnd);

                return ProcessIsElavatedByHandle(hProcess);
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 0x5)
            {
                // Access is denied to the process's handle.
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool ProcessIsElavatedByHandle(HANDLE hProcess)
        {
            var phToken = PHANDLE.Zero;
            try
            {
                NativeMethods.OpenProcessToken(hProcess, 8, out phToken);
                return new WindowsIdentity(phToken).IsAdministrator();
            }
            catch
            {
                return false;
            }
            finally
            {
                if (phToken != PHANDLE.Zero)
                {
                    NativeMethods.CloseHandle(phToken);
                }
            }
        }

        public static bool ProcessIsElevated(Process p)
        {
            try
            {
                // Should throw access denied if is admin and we aren't
                // in this case if we are admin, we still want to check if it
                // is running as admin, as it will not throw anymore :)
                return ProcessIsElavatedByHandle(p.Handle);
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 0x5)
            {
                // Access is denied to the process's handle.
                return true;
            }
        }

        public static bool GetProcessByName(string processName, out Process p)
        {
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length > 0)
            {
                p = processes[0];
                return true;
            }

            p = null;

            return false;
        }

        public static HWND GetWindowHandle(string processName)
        {
            var processesByName = Process.GetProcessesByName(processName);

            if (processesByName.Length <= LastProcessID - 1)
            {
                return HWND.Zero;
            }

            if (processesByName.Length == 0)
            {
                return HWND.Zero;
            }

            GetMainWindowHandleForProcess(processesByName, out HANDLE mainWindowHandle, out int mainWindowHandle32);

            if (mainWindowHandle32 > 0)
            {
                LastProcessID = 0;
                return mainWindowHandle;
            }

            LastProcessID++;
            return GetWindowHandle(processName);
        }

        private static void GetMainWindowHandleForProcess(Process[] processesByName, out HANDLE mainWindowHandle, out int mainWindowHandle32)
        {
            Process process;

            if (LastProcessID > processesByName.Length - 1)
                process = processesByName[processesByName.Length - 1];
            else process = processesByName[LastProcessID];

            mainWindowHandle = process.MainWindowHandle;
            mainWindowHandle32 = mainWindowHandle.ToInt32();
        }

        public static int LastProcessID = 0;
    }
}
