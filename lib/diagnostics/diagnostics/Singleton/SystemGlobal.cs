using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using MFDLabs.Networking;
using MFDLabs.Text.Extensions;

#if NETFRAMEWORK
using System.Security.Principal;
using MFDLabs.Diagnostics.Extensions;
#else
using System.Runtime.InteropServices;
#endif

namespace MFDLabs.Diagnostics
{
    public static class SystemGlobal
    {
        public static Process CurrentProcess { get; } = Process.GetCurrentProcess();
        public static string CurrentPlatform { get; } = DevicePlatformHelper.CurrentDevicePlatform();
        public static string CurrentDeviceArch { get; } = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
        public static string Version { get; } = Environment.Version.ToString();
        public static string AssemblyVersion { get; } = Assembly.GetEntryAssembly()?.GetName().Version?.ToString();

        public static string GetMachineHost()
        {
            return !global::Diagnostics.Properties.Settings.Default.MachineHostOverride
                .IsNullWhiteSpaceOrEmpty()
                ? global::Diagnostics.Properties.Settings.Default.MachineHostOverride
                : Dns.GetHostEntry(NetworkingGlobal.GetLocalIp()).HostName;
        }

        public static string GetMachineId()
        {
            return !global::Diagnostics.Properties.Settings.Default.MachineIDOverride.IsNullWhiteSpaceOrEmpty()
                ? global::Diagnostics.Properties.Settings.Default.MachineIDOverride
                : Environment.MachineName;
        }

#if !NETFRAMEWORK
        [DllImport("libc")]
        public static extern uint getuid();
#endif
        
        // TODO: Pull out to MFDLabs.Security
        public static bool ContextIsAdministrator()
        {
#if NETFRAMEWORK
            return WindowsIdentity.GetCurrent().IsAdministrator();
#else

            return getuid() == 0;
#endif
        }
        
    }
}
