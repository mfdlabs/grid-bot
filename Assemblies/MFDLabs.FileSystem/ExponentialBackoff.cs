using System;
using MFDLabs.Threading;

namespace MFDLabs.FileSystem
{
    public enum Jitter
    {
        None,
        Full,
        Equal
    }

    internal static class ExponentialBackoff
    {
        private const uint CeilingForMaxAttempts = 10;
        private static readonly ThreadLocalRandom Random = new();

        public static TimeSpan Multiply(this TimeSpan multiplicand, double multiplier)
            => TimeSpan.FromTicks((long)(multiplicand.Ticks * multiplier));

        public static TimeSpan CalculateBackoff(uint attempt, uint maxAttempts, TimeSpan baseDelay, TimeSpan maxDelay, Jitter jitter = Jitter.None) 
            => CalculateBackoff(attempt, maxAttempts, baseDelay, maxDelay, jitter, () => Random.NextDouble());

        private static TimeSpan CalculateBackoff(uint attempt, uint maxAttempts, TimeSpan baseDelay, TimeSpan maxDelay, Jitter jitter, Func<double> nextRandomDouble)
        {
            if (maxAttempts > CeilingForMaxAttempts) 
                throw new ArgumentOutOfRangeException($"{maxAttempts} must be less than or equal to {CeilingForMaxAttempts}");
            if (attempt > maxAttempts) attempt = maxAttempts;
            var delay = baseDelay.Multiply(Math.Pow(2, attempt - 1));
            if (delay > maxDelay || delay < TimeSpan.Zero) delay = maxDelay;
            var random = nextRandomDouble();
            delay = jitter switch
            {
                Jitter.Full => delay.Multiply(random),
                Jitter.Equal => delay.Multiply(0.5 + random * 0.5),
                _ => delay
            };
            return delay;
        }
    }
}
