#if NETFRAMEWORK // This will be removed if we can find a way of checkin on unix if an app is running as sudo!

using System.Diagnostics;

namespace MFDLabs.Diagnostics.Extensions
{
    public static class ProcessExtensions
    {
        public static bool IsElevated(this Process self) => ProcessHelper.ProcessIsElevated(self);
    }
}

#endif