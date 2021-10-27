namespace MFDLabs.Networking.Extensions
{
    public static class NetworkingExtensions
    {
        public static long ToIntIpAddress(this string ip) => NetworkingGlobal.Singleton.ToInt(ip);
        public static string ToIpAddress(this long ip) => NetworkingGlobal.Singleton.ToAddress(ip);
        public static string ToIpAddress(this int ip) => NetworkingGlobal.Singleton.ToAddress(ip);
    }
}
