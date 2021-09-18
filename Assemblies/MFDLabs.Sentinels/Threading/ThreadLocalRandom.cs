using System;
using System.Threading;

namespace MFDLabs.Sentinels
{
    internal class ThreadLocalRandom
    {
        public ThreadLocalRandom(int initialSeed)
        {
            _Seed = initialSeed;
            _Random = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref _Seed)));
        }

        public ThreadLocalRandom() : this(Environment.TickCount)
        {
        }

        public int Next()
        {
            return _Random.Value.Next();
        }

        public int Next(int maxValue)
        {
            return _Random.Value.Next(maxValue);
        }

        public int Next(int minValue, int maxValue)
        {
            return _Random.Value.Next(minValue, maxValue);
        }

        public double NextDouble()
        {
            return _Random.Value.NextDouble();
        }

        private readonly ThreadLocal<Random> _Random;

        private int _Seed;
    }
}
