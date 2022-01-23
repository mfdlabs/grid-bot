using System.Diagnostics;
using System.Collections.Generic;
using MFDLabs.Text.Extensions;

using HANDLE = System.IntPtr;
using HWND = System.IntPtr;

#if NETFRAMEWORK
using System;
using System.ComponentModel;
using System.Security.Principal;
using System.Management;
using MFDLabs.Diagnostics.Extensions;
using MFDLabs.Diagnostics.NativeWin32;

using PHANDLE = System.IntPtr;
#endif

namespace MFDLabs.Diagnostics
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
            var processHandle = PHANDLE.Zero;
            try
            {
                NativeMethods.OpenProcessToken(process.Handle, 0x8, out processHandle);
                var wi = new WindowsIdentity(processHandle);
                string user = wi.Name;
                return user.Contains(@"\") ? user.Substring(user.IndexOf(@"\") + 1) : user;
            }
            catch
            {
                return null;
            }
            finally
            {
                if (processHandle != IntPtr.Zero) 
                    NativeMethods.CloseHandle(processHandle);
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

            GetMainWindowHandleForProcess(processesByName, out HANDLE mainWindowHandle, out int mainWindowHandle32);

            if (mainWindowHandle32 > 0)
            {
                _lastProcessId = 0;
                return mainWindowHandle;
            }

            _lastProcessId++;
            return GetWindowHandle(processName);
        }

        private static void GetMainWindowHandleForProcess(IReadOnlyList<Process> processesByName, out HANDLE mainWindowHandle, out int mainWindowHandle32)
        {
            var process = _lastProcessId > processesByName.Count - 1 ? processesByName[processesByName.Count - 1] : processesByName[_lastProcessId];

            mainWindowHandle = process.MainWindowHandle;
            mainWindowHandle32 = mainWindowHandle.ToInt32();
        }

        private static int _lastProcessId;
    }
}
