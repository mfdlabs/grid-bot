using System.Diagnostics;

namespace Diagnostics.Extensions
{
    public static class ProcessExtensions
    {
        public static bool IsElevated(this Process self) => ProcessHelper.ProcessIsElevated(self);
        public static string GetOwner(this Process self) => ProcessHelper.GetProcessOwnerByProcess(self);
    }
}
