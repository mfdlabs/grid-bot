using System;

namespace Diagnostics
{
    public static class DevicePlatformHelper
    {
        public static string CurrentDevicePlatform()
        {
            return Environment.OSVersion.Platform switch
            {
                PlatformID.Win32NT => "win32nt",
                PlatformID.Win32S => "win32s",
                PlatformID.Win32Windows => "win32l",
                PlatformID.WinCE => "wince",
                PlatformID.Xbox => "durango",
                PlatformID.Unix => "unix",
                PlatformID.MacOSX => "macosx",
                _ => "unknown"
            };
        }
    }
}
