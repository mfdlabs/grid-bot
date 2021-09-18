namespace MFDLabs.Diagnostics.Extensions
{
    public static unsafe class PointerHelpersExtensions
    {
        public static T* ToPtr<T>(this T self)
            where T : unmanaged
        {
            return PointerHelpers.Singleton.ToPointer<T>(self);
        }
    }
}
