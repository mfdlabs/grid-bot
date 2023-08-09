namespace MFDLabs.Diagnostics
{
    public static unsafe class PointerHelpers
    {
        public static T* ToPointer<T>(T u)
            where T : unmanaged =>
            &u;

        public static T FromPointer<T>(T* r)
            where T : unmanaged
        {
            return *r;
        }
    }
}
