using System.Runtime.InteropServices;

namespace Diagnostics
{
    public static class Marshaler
    {
        public static int SizeOf<T>()
            where T : struct =>
            Marshal.SizeOf(default(T));
    }
}
