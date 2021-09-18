using MFDLabs.Abstractions;
using System.Runtime.InteropServices;

namespace MFDLabs.Diagnostics
{
    public sealed class Marshaler : SingletonBase<Marshaler>
    {
        public int SizeOf<T>() 
            where T : struct
        {
            return Marshal.SizeOf(default(T));
        }
    }
}
