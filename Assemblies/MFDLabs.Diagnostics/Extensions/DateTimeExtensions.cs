using System;

namespace MFDLabs.Diagnostics.Extensions
{
    public static class DateTimeExtensions
    {
        public static string ToIso(this DateTime self) => self.ToString("yyyy-MM-ddTHH:mm:ss.ffffZ");
    }
}
