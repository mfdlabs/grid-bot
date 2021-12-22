using System.Runtime.InteropServices;

namespace MFDLabs.Diagnostics
{
    public static class Marshaler
    {
        public static int SizeOf<T>()
            where T : struct =>
            Marshal.SizeOf(default(T));
    }
}
