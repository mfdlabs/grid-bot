namespace Grid;

using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Runtime.InteropServices;

using PInvoke;

using Win32Exception = System.ComponentModel.Win32Exception;

internal static class ProcessExtensions
{
    public static string GetOwner(this Process process)
    {
        var tkHandle = Kernel32.SafeObjectHandle.Null;
        try
        {
            AdvApi32.OpenProcessToken(process.Handle, 0x8, out tkHandle);

            if (tkHandle.IsInvalid)
                return null;

            var wi = new WindowsIdentity(tkHandle.DangerousGetHandle());
            string user = wi.Name;
            return user.Contains(@"\") ? user.Substring(user.IndexOf(@"\") + 1) : user;
        }
        catch
        {
            return null;
        }
        finally
        {
            if (tkHandle != Kernel32.SafeObjectHandle.Null)
                tkHandle.Close();
        }
    }

    public static bool SafeGetHasExited(this Process process)
    {
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
