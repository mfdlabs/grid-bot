using MFDLabs.Abstractions;

namespace MFDLabs.Diagnostics
{
    public sealed unsafe class PointerHelpers : SingletonBase<PointerHelpers>
    {
        public T* ToPointer<T>(T u)
            where T : unmanaged
        {
            return &u;
        }

        public T FromPointer<T>(T* r)
            where T : unmanaged
        {
            return *r;
        }
    }
}
