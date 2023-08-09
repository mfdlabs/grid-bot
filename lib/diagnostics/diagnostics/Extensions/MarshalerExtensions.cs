namespace MFDLabs.Diagnostics.Extensions
{
    public static class MarshalerExtensions
    {
        public static int SizeOf<T>(this T _)
            where T : struct =>
            Marshaler.SizeOf<T>();
    }
}
