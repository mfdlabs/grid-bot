using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Text.RegularExpressions;
using MFDLabs.Diagnostics.Extensions;
using MFDLabs.Diagnostics.NativeWin32;
using MFDLabs.Text.Extensions;

using HANDLE = System.IntPtr;
using HWND = System.IntPtr;
using PHANDLE = System.IntPtr;

namespace MFDLabs.Diagnostics
{
    public class ProcessHelper
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

        public static bool GetProcessByTcpPortAndName(string name, int port, out Process pr)
        {
            pr = null;
            // We can do Win32 magic later, for now just use p-ports
            /*NativeMethods.GetTcpStatistics(out var stats);*/
            foreach (var p in ProcessPorts.ProcessPortMap.FindAll(x => x.ProcessName.ToLower() == name && x.PortNumber == port && x.Protocol.ToLower().Contains("tcp")))
            {
                pr = Process.GetProcessById(p.ProcessId);
                return true;
            }
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

    /// <summary>
    /// Static class that returns the list of processes and the ports those processes use.
    /// </summary>
    public static class ProcessPorts
    {
        /// <summary>
        /// A list of ProcesesPorts that contain the mapping of processes and the ports that the process uses.
        /// </summary>
        public static List<ProcessPort> ProcessPortMap
        {
            get
            {
                return GetNetStatPorts();
            }
        }


        /// <summary>
        /// This method distills the output from netstat -a -n -o into a list of ProcessPorts that provide a mapping between
        /// the process (name and id) and the ports that the process is using.
        /// </summary>
        /// <returns></returns>
        private static List<ProcessPort> GetNetStatPorts()
        {
            List<ProcessPort> ProcessPorts = new List<ProcessPort>();

            try
            {
                using (Process Proc = new Process())
                {

                    ProcessStartInfo StartInfo = new ProcessStartInfo
                    {
                        FileName = "netstat.exe",
                        Arguments = "-a -n -o",
                        WindowStyle = ProcessWindowStyle.Hidden,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    Proc.StartInfo = StartInfo;
                    Proc.Start();

                    StreamReader StandardOutput = Proc.StandardOutput;
                    StreamReader StandardError = Proc.StandardError;

                    string NetStatContent = StandardOutput.ReadToEnd() + StandardError.ReadToEnd();
                    string NetStatExitStatus = Proc.ExitCode.ToString();

                    if (NetStatExitStatus != "0")
                    {
                        Console.WriteLine("NetStat command failed.   This may require elevated permissions.");
                    }

                    string[] NetStatRows = Regex.Split(NetStatContent, "\r\n");

                    foreach (string NetStatRow in NetStatRows)
                    {
                        string[] Tokens = Regex.Split(NetStatRow, "\\s+");
                        if (Tokens.Length > 4 && (Tokens[1].Equals("UDP") || Tokens[1].Equals("TCP")))
                        {
                            string IpAddress = Regex.Replace(Tokens[2], @"\[(.*?)\]", "1.1.1.1");
                            try
                            {
                                ProcessPorts.Add(new ProcessPort(
                                    Tokens[1] == "UDP" ? GetProcessName(Convert.ToInt16(Tokens[4])) : GetProcessName(Convert.ToInt16(Tokens[5])),
                                    Tokens[1] == "UDP" ? Convert.ToInt16(Tokens[4]) : Convert.ToInt16(Tokens[5]),
                                    IpAddress.Contains("1.1.1.1") ? String.Format("{0}v6", Tokens[1]) : String.Format("{0}v4", Tokens[1]),
                                    Convert.ToInt32(IpAddress.Split(':')[1])
                                ));
                            }
                            catch
                            {
                                Console.WriteLine("Could not convert the following NetStat row to a Process to Port mapping.");
                                Console.WriteLine(NetStatRow);
                            }
                        }
                        else
                        {
                            if (!NetStatRow.Trim().StartsWith("Proto") && !NetStatRow.Trim().StartsWith("Active") && !string.IsNullOrWhiteSpace(NetStatRow))
                            {
                                Console.WriteLine("Unrecognized NetStat row to a Process to Port mapping.");
                                Console.WriteLine(NetStatRow);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return ProcessPorts;
        }

        /// <summary>
        /// Private method that handles pulling the process name (if one exists) from the process id.
        /// </summary>
        /// <param name="ProcessId"></param>
        /// <returns></returns>
        private static string GetProcessName(int ProcessId)
        {
            string procName = "UNKNOWN";

            try
            {
                procName = Process.GetProcessById(ProcessId).ProcessName;
            }
            catch { }

            return procName;
        }
    }

    /// <summary>
    /// A mapping for processes to ports and ports to processes that are being used in the system.
    /// </summary>
    public class ProcessPort
    {
        private readonly string _ProcessName = string.Empty;
        private readonly int _ProcessId = 0;
        private readonly string _Protocol = string.Empty;
        private readonly int _PortNumber = 0;

        /// <summary>
        /// Internal constructor to initialize the mapping of process to port.
        /// </summary>
        /// <param name="ProcessName">Name of process to be </param>
        /// <param name="ProcessId"></param>
        /// <param name="Protocol"></param>
        /// <param name="PortNumber"></param>
        internal ProcessPort(string ProcessName, int ProcessId, string Protocol, int PortNumber)
        {
            _ProcessName = ProcessName;
            _ProcessId = ProcessId;
            _Protocol = Protocol;
            _PortNumber = PortNumber;
        }

        public string ProcessPortDescription
        {
            get
            {
                return string.Format("{0} ({1} port {2} pid {3})", _ProcessName, _Protocol, _PortNumber, _ProcessId);
            }
        }
        public string ProcessName
        {
            get { return _ProcessName; }
        }
        public int ProcessId
        {
            get { return _ProcessId; }
        }
        public string Protocol
        {
            get { return _Protocol; }
        }
        public int PortNumber
        {
            get { return _PortNumber; }
        }
    }
}
