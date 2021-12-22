#if NETFRAMEWORK

using System.Diagnostics;

namespace MFDLabs.Diagnostics.Extensions
{
    public static class ProcessExtensions
    {
        public static bool IsElevated(this Process self) => ProcessHelper.ProcessIsElevated(self);
    }
}

#endif