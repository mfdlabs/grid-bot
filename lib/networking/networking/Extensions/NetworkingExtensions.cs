namespace Networking.Extensions
{
    public static class NetworkingExtensions
    {
        public static long ToIntIpAddress(this string ip) => NetworkingGlobal.ToInt(ip);
        public static string ToIpAddress(this long ip) => NetworkingGlobal.ToAddress(ip);
        public static string ToIpAddress(this int ip) => NetworkingGlobal.ToAddress(ip);
        public static bool IsIpV4(this string ip) => NetworkingGlobal.IsIpv4(ip);
        public static bool IsIpV6(this string ip) => NetworkingGlobal.IsIpv6(ip);
        public static bool IsInIpRange(this string ip, string range) => NetworkingGlobal.IsIpInRange(ip, range);
        public static bool IsInIpRanges(this string ip, string[] ranges) => NetworkingGlobal.IsIpInRangeList(ip, ranges);
        public static bool IsInNetmask(this string ip, string netmask) => NetworkingGlobal.IsIpInNetmask(ip, netmask);
        public static bool IsInNetmasks(this string ip, string[] netmasks) => NetworkingGlobal.IsIpInNetmaskList(ip, netmasks);
        public static bool IsInCidrRange(this string ip, string cidr) => NetworkingGlobal.IsIpInCidrRange(ip, cidr);
        public static bool IsInCidrRanges(this string ip, string[] cidrs) => NetworkingGlobal.IsIpInCidrRangeList(ip, cidrs);
        public static bool IsInRanges(this string ip, string[] ranges) => NetworkingGlobal.IsIpInCidrNetmaskOrRangeList(ip, ranges);
    }
}
