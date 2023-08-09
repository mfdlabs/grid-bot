using System.Diagnostics;
using Text.Extensions;

#if NETFRAMEWORK
using System;
using System.Management;
using System.Security.Principal;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using PInvoke;
using Diagnostics.Extensions;
using Diagnostics.NativeWin32;

// WinAPI members
using PHANDLE = System.IntPtr;
using HANDLE = System.IntPtr;
using HWND = System.IntPtr;
using DWORD = System.Int32;
using Win32Exception = System.ComponentModel.Win32Exception;
#endif

namespace Diagnostics
{
    public static class ProcessHelper
    {
        public static bool GetProcessById(int pid, out Process pr)
        {
            pr = null;
            try
            {
                pr = Process.GetProcessById(pid);
                return true;
            }
            catch
            {
                return false;
            }
        }

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

#if NETFRAMEWORK
        public static bool TryEnableAnsiCodesForHandle(Kernel32.StdHandle stdHandle)
        {
            var consoleHandle = Kernel32.GetStdHandle(stdHandle);
            if (Kernel32.GetConsoleMode(consoleHandle, out var consoleBufferModes) &&
                consoleBufferModes.HasFlag(Kernel32.ConsoleBufferModes.ENABLE_VIRTUAL_TERMINAL_PROCESSING))
                return true;

            consoleBufferModes |= Kernel32.ConsoleBufferModes.ENABLE_VIRTUAL_TERMINAL_PROCESSING;
            return Kernel32.SetConsoleMode(consoleHandle, consoleBufferModes);
        }
#endif

        public static string GetCurrentUser()
        {
#if NETFRAMEWORK || WE_LOVE_ENVIRONMENT_USERNAME
            return Environment.UserName;
#else
            var command = $"whoami";
            var startInfo = new ProcessStartInfo() { FileName = "/bin/bash", Arguments = command, };
            var proc = new Process() { StartInfo = startInfo, };
            proc.Start();
            proc.WaitForExit();

            var user = proc.StandardOutput.ReadToEnd();

            return user.IsNullOrEmpty() ? null : user;
#endif
        }

        public static string GetProcessOwnerByProcess(Process process)
        {
#if NETFRAMEWORK
#if !SIMPLISTIC_FETCH_OF_OWNER
            var processHandle = Kernel32.SafeObjectHandle.Null;
            var phandle = PHANDLE.Zero;
            try
            {
                AdvApi32.OpenProcessToken(process.Handle, 0x8, out processHandle);

                if (processHandle.IsInvalid)
                    return null;

                phandle = processHandle.DangerousGetHandle();

                var wi = new WindowsIdentity(phandle);
                string user = wi.Name;
                return user.Contains(@"\") ? user.Substring(user.IndexOf(@"\") + 1) : user;
            }
            catch
            {
                return null;
            }
            finally
            {
                if (phandle != PHANDLE.Zero) 
                    Kernel32.CloseHandle(phandle);
            }
#else
            return GetProcessOwnerByProcessId(process.Id);
#endif
#else
            return ProcessHelper.GetProcessOwnerByProcessId(process.Id);
#endif
        }

        public static string GetProcessOwnerByProcessName(string processName)
        {
#if NETFRAMEWORK
            var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_Process WHERE Name = \"{processName}\"");

            foreach (ManagementObject obj in searcher.Get())
            {
                var argList = new string[] { string.Empty, string.Empty };
                var returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                if (returnVal == 0)
                {
                    // return DOMAIN\user
                    return $"{argList[1]}\\{argList[0]}";
                }
            }

            return null;
#else
            var command = $"ps -o user= -C \"{processName}\"";
            var startInfo = new ProcessStartInfo() { FileName = "/bin/bash", Arguments = command, };
            var proc = new Process() { StartInfo = startInfo, };
            proc.Start();
            proc.WaitForExit();

            var owner = proc.StandardOutput.ReadToEnd();

            return owner.IsNullOrEmpty() ? null : owner;
#endif
        }

        public static string GetProcessOwnerByProcessId(int processId)
        {
#if NETFRAMEWORK
            var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_Process WHERE ProcessID = {processId}");

            foreach (ManagementObject obj in searcher.Get())
            {
                var argList = new string[] { string.Empty, string.Empty };
                var returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                if (returnVal == 0)
                {
                    // return DOMAIN\user
                    return $"{argList[1]}\\{argList[0]}";
                }
            }

            return null;
#else
            var command = $"ps -o user= -p {processId}";
            var startInfo = new ProcessStartInfo() { FileName = "/bin/bash", Arguments = command, };
            var proc = new Process() { StartInfo = startInfo, };
            proc.Start();
            proc.WaitForExit();

            var owner = proc.StandardOutput.ReadToEnd();

            return owner.IsNullOrEmpty() ? null : owner;
#endif
        }

        public static bool ProcessIsElevatedByProcessId(int processId)
        {
#if NETFRAMEWORK
            try
            {
                var process = Process.GetProcessById(processId);

                return ProcessIsElavatedByHandle(process.Handle);
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 0x5)
            {
                // Access denied
                return true;
            }
            catch
            {
                return false;
            }
#else
            var owner = GetProcessOwnerByProcessId(processId);
            var user = GetCurrentUser();

            return owner != user && user != "root"; // ?? Maybe
#endif
        }

#if NETFRAMEWORK

        public static bool ProcessIsElevatedByWindowHandle(HWND hwnd)
        {
            try
            {
                var hProcess = Oleacc.GetProcessHandleFromHwnd(hwnd);

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
            var processHandle = Kernel32.SafeObjectHandle.Null;
            var phandle = PHANDLE.Zero;
            try
            {
                AdvApi32.OpenProcessToken(hProcess, 8, out processHandle);

                if (processHandle.IsInvalid)
                    return false;

                phandle = processHandle.DangerousGetHandle();

                return new WindowsIdentity(phandle).IsAdministrator();
            }
            catch
            {
                return false;
            }
            finally
            {
                if (phandle != PHANDLE.Zero)
                    Kernel32.CloseHandle(phandle);
            }
        }


#endif

        public static bool ProcessIsElevated(Process p)
        {
#if NETFRAMEWORK
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
#else
            return ProcessIsElevatedByProcessId(p.Id);
#endif
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

#if NETFRAMEWORK

        [Obsolete("Just use Process.MainWindowHandle")]
        public static HWND GetWindowHandleLegacy(DWORD dwProcessId)
        {
            var process = Process.GetProcessById(dwProcessId);
            if (process == null) return HWND.Zero;

            GetHWNDByProcess(out HWND hWnd, out DWORD dwMainWindowHandle32, ref process);

            if (dwMainWindowHandle32 > 0) return hWnd;

            return HWND.Zero;
        }

        public static HWND GetWindowHandle(string processName)
        {
            var processesByName = Process.GetProcessesByName(processName);

            if (processesByName.Length <= _lastProcessId - 1)
            {
                return HWND.Zero;
            }

            if (processesByName.Length == 0)
            {
                return HWND.Zero;
            }

            GetMainWindowHandleForProcess(processesByName, out HWND mainWindowHandle, out int mainWindowHandle32);

            if (mainWindowHandle32 > 0)
            {
                _lastProcessId = 0;
                return mainWindowHandle;
            }

            _lastProcessId++;
            return GetWindowHandle(processName);
        }

        private static void GetMainWindowHandleForProcess(IReadOnlyList<Process> processesByName, out HWND mainWindowHandle, out DWORD dwMainWindowHandle32)
        {
            var process = _lastProcessId > processesByName.Count - 1 ? processesByName[processesByName.Count - 1] : processesByName[_lastProcessId];
            GetHWNDByProcess(out mainWindowHandle, out dwMainWindowHandle32, ref process);
        }

        private static void GetHWNDByProcess(out HWND mainWindowHandle, out DWORD dwMainWindowHandle32, [In] ref Process process)
        {
            mainWindowHandle = process.MainWindowHandle;
            dwMainWindowHandle32 = mainWindowHandle.ToInt32();
        }

        private static int _lastProcessId;

#endif
    }
}
