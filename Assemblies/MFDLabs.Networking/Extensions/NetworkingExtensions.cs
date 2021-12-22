namespace MFDLabs.Networking.Extensions
{
    public static class NetworkingExtensions
    {
        public static long ToIntIpAddress(this string ip) => NetworkingGlobal.ToInt(ip);
        public static string ToIpAddress(this long ip) => NetworkingGlobal.ToAddress(ip);
        public static string ToIpAddress(this int ip) => NetworkingGlobal.ToAddress(ip);
    }
}
