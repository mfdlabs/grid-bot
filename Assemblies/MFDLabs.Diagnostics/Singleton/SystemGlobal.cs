using MFDLabs.Abstractions;
using MFDLabs.Networking;
using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Security.Principal;

namespace MFDLabs.Diagnostics
{
    public sealed class SystemGlobal : SingletonBase<SystemGlobal>
    {
        private readonly Process _thisProcess;
        private readonly string _currentPlatform;
        private readonly string _currentArch;
        private readonly string _envVersion;
        private readonly string _assemblyVersion;

        public Process CurrentProcess
        {
            get { return _thisProcess; }
        }

        public string CurrentPlatform
        {
            get { return _currentPlatform; }
        }

        public string CurrentDeviceArch
        {
            get { return _currentArch; }
        }

        public string Version
        {
            get { return _envVersion; }
        }

        public string AssemblyVersion
        {
            get { return _assemblyVersion; }
        }

        public SystemGlobal()
        {
            _thisProcess = Process.GetCurrentProcess();
            _currentPlatform = DevicePlatformHelper.CurrentDevicePlatform();
            _currentArch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
            _envVersion = Environment.Version.ToString();
            _assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        public string GetMachineHost()
        {
            return Dns.GetHostEntry(NetworkingGlobal.Singleton.GetLocalIP()).HostName;
        }

        public string GetMachineID()
        {
            return Environment.MachineName;
        }

        // TODO: Pull out to MFDLabs.Security
        public bool ContextIsAdministrator()
        {
            return ContextIsAdministrator(WindowsIdentity.GetCurrent());
        }

        public bool ContextIsAdministrator(WindowsIdentity id)
        {
            return new WindowsPrincipal(id)
                      .IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
