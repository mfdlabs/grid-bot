using System;

namespace MFDLabs.Sentinels
{
    internal static class TimeSpanExtensions
    {
        public static TimeSpan Multiply(this TimeSpan multiplicand, double multiplier)
        {
            return TimeSpan.FromTicks((long)(multiplicand.Ticks * multiplier));
        }
    }
}
