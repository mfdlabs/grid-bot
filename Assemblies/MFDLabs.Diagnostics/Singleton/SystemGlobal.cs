using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using MFDLabs.Networking;
using MFDLabs.Text.Extensions;

#if NETFRAMEWORK
using System.Security.Principal;
using MFDLabs.Diagnostics.Extensions;
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
            return !global::MFDLabs.Diagnostics.Properties.Settings.Default.MachineHostOverride
                .IsNullWhiteSpaceOrEmpty()
                ? global::MFDLabs.Diagnostics.Properties.Settings.Default.MachineHostOverride
                : Dns.GetHostEntry(NetworkingGlobal.GetLocalIp()).HostName;
        }

        public static string GetMachineId()
        {
            return !global::MFDLabs.Diagnostics.Properties.Settings.Default.MachineIDOverride.IsNullWhiteSpaceOrEmpty()
                ? global::MFDLabs.Diagnostics.Properties.Settings.Default.MachineIDOverride
                : Environment.MachineName;
        }

#if NETFRAMEWORK

        // TODO: Pull out to MFDLabs.Security
        public static bool ContextIsAdministrator()
        {
            return WindowsIdentity.GetCurrent().IsAdministrator();
        }

#endif
    }
}