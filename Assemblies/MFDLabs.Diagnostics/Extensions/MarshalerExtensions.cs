namespace MFDLabs.Diagnostics.Extensions
{
    public static class MarshalerExtensions
    {
        public static int SizeOf<T>(this T _)
            where T : struct
        {
            return Marshaler.Singleton.SizeOf<T>();
        }
    }
}
