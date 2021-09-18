using System;

namespace MFDLabs.Diagnostics
{
    public class DevicePlatformHelper
    {
        public static string CurrentDevicePlatform()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    return "win32nt";
                case PlatformID.Win32S:
                    return "win32s";
                case PlatformID.Win32Windows:
                    return "win32l";
                case PlatformID.WinCE:
                    return "wince";
                case PlatformID.Xbox:
                    return "durango";
                case PlatformID.Unix:
                    return "unix";
                case PlatformID.MacOSX:
                    return "macosx";
                default:
                    return "unknown";
            }
        }
    }
}
