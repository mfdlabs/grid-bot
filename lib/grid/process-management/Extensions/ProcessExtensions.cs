namespace Grid.ProcessManagement;

using System;
using System.Net;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;

using Win32Exception = System.ComponentModel.Win32Exception;

internal static class ProcessExtensions
{
    public static bool GetProcessEndPoint(this Process process, out IPEndPoint endPoint)
    {
        endPoint = null;

        var row = ManagedIpHelper.GetExtendedTcpTable(true)
            .FirstOrDefault(r => r.ProcessId == process.Id && r.LocalEndPoint.Address.Equals(IPAddress.Loopback));

        if (row != null)
        {
            endPoint = row.LocalEndPoint;

            return true;
        }

        return false;
    }

    public static bool SafeGetHasExited(this Process process)
    {
        if (process == null) return true;

        SafeFileHandle hProcess = null;

        try
        {
            hProcess = PInvoke.OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, (uint)process.Id);
            if (hProcess.IsInvalid)
                return true;

            return PInvoke.GetExitCodeProcess(hProcess, out var lpExitCode) && lpExitCode != 259;
        }
        finally
        {
            if (!hProcess.IsInvalid && !hProcess.IsClosed)
                hProcess.Close();
        }
    }

    public static (bool, WIN32_ERROR) ForceKill(this Process proc)
    {
        if (proc == null || proc.SafeGetHasExited())
            return (false, WIN32_ERROR.ERROR_PROCESS_ABORTED);

        var hProcess = PInvoke.OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_TERMINATE, false, (uint)proc.Id);
        if (hProcess.IsInvalid) 
            return (false, (WIN32_ERROR)Marshal.GetLastWin32Error());

        if (!PInvoke.TerminateProcess(hProcess, 0)) 
            return (false, (WIN32_ERROR)Marshal.GetLastWin32Error());

        hProcess.Close();

        return (true, WIN32_ERROR.NO_ERROR);
    }
}
