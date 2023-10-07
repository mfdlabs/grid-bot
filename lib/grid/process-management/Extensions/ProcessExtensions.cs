namespace Grid;

using System;
using System.Net;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;

using PInvoke;

using Win32Exception = System.ComponentModel.Win32Exception;


internal static class ProcessExtensions
{
    public static bool GetProcessEndPoint(this Process process, out IPEndPoint endPoint)
    {
        endPoint = null;

        var row = ManagedIpHelper.GetExtendedTcpTable(true)
            .FirstOrDefault(r => r.ProcessId == process.Id);

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

        var hProcess = Kernel32.SafeObjectHandle.Null;

        try
        {
            var processHandle = Kernel32.OpenProcess(Kernel32.ProcessAccess.PROCESS_QUERY_LIMITED_INFORMATION, false, process.Id);
            if (processHandle == Kernel32.SafeObjectHandle.Null || processHandle.IsInvalid)
                return true;

            return Kernel32.GetExitCodeProcess(processHandle.DangerousGetHandle(), out var lpExitCode) && lpExitCode != 259;
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException or Win32Exception or COMException)
        {
            return false; // Handle either exists and we don't have access, or it doesn't exist. Assume exists.
        }
        finally
        {
            if (hProcess != Kernel32.SafeObjectHandle.Null)
                hProcess.Close();
        }
    }

    public static (bool, Win32ErrorCode) ForceKill(this Process proc)
    {
        if (proc == null || proc.SafeGetHasExited())
            return (false, Win32ErrorCode.ERROR_PROCESS_ABORTED);

        var objHandle = Kernel32.OpenProcess(Kernel32.ProcessAccess.PROCESS_TERMINATE, false, proc.Id);
        if (objHandle == Kernel32.SafeObjectHandle.Null) 
            return (false, Kernel32.GetLastError());

        if (!Kernel32.TerminateProcess(objHandle.DangerousGetHandle(), 0)) 
            return (false, Kernel32.GetLastError());

        objHandle.Close();

        return (true, Win32ErrorCode.NERR_Success);
    }
}
