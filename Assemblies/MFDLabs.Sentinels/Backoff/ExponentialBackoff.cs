using System;
using MFDLabs.Threading;

namespace MFDLabs.Sentinels
{
    public static class ExponentialBackoff
    {
        public static TimeSpan CalculateBackoff(byte attempt, byte maxAttempts, TimeSpan baseDelay, TimeSpan maxDelay, Jitter jitter = Jitter.None) 
            => CalculateBackoff(attempt, maxAttempts, baseDelay, maxDelay, jitter, () => Random.NextDouble());

        private static TimeSpan CalculateBackoff(byte attempt, byte maxAttempts, TimeSpan baseDelay, TimeSpan maxDelay, Jitter jitter, Func<double> nextRandomDouble)
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

        private const byte CeilingForMaxAttempts = 10;
        private static readonly ThreadLocalRandom Random = new ThreadLocalRandom();
    }
}
