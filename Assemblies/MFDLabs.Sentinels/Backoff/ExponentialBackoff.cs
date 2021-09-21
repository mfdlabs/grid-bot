using System;
using MFDLabs.Threading;

namespace MFDLabs.Sentinels
{
    public static class ExponentialBackoff
    {
        public static TimeSpan CalculateBackoff(byte attempt, byte maxAttempts, TimeSpan baseDelay, TimeSpan maxDelay, Jitter jitter = Jitter.None)
        {
            return CalculateBackoff(attempt, maxAttempts, baseDelay, maxDelay, jitter, () => _Random.NextDouble());
        }

        internal static TimeSpan CalculateBackoff(byte attempt, byte maxAttempts, TimeSpan baseDelay, TimeSpan maxDelay, Jitter jitter, Func<double> nextRandomDouble)
        {
            if (maxAttempts > _CeilingForMaxAttempts)
            {
                throw new ArgumentOutOfRangeException($"{maxAttempts} must be less than or equal to {_CeilingForMaxAttempts}");
            }
            if (attempt > maxAttempts)
            {
                attempt = maxAttempts;
            }
            var delay = baseDelay.Multiply(Math.Pow(2.0, attempt - 1));
            if (delay > maxDelay || delay < TimeSpan.Zero)
            {
                delay = maxDelay;
            }
            var random = nextRandomDouble();
            switch (jitter)
            {
                case Jitter.Full:
                    delay = delay.Multiply(random);
                    break;
                case Jitter.Equal:
                    delay = delay.Multiply(0.5 + random * 0.5);
                    break;
            }
            return delay;
        }

        private const byte _CeilingForMaxAttempts = 10;

        private static readonly ThreadLocalRandom _Random = new ThreadLocalRandom();
    }
}
