using System.Diagnostics;

namespace MFDLabs.Diagnostics.Extensions
{
    public static class ProcessExtensions
    {
        public static bool IsElevated(this Process self)
        {
            return ProcessHelper.ProcessIsElevated(self);
        }
    }
}
