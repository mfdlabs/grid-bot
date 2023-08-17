namespace Diagnostics.Extensions
{
    public static unsafe class PointerHelpersExtensions
    {
        public static T* ToPtr<T>(this T self)
            where T : unmanaged =>
            PointerHelpers.ToPointer(self);
    }
}
