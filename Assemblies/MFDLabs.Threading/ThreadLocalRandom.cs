using System;
using System.Threading;

namespace MFDLabs.Threading
{
    public class ThreadLocalRandom
    {
        public ThreadLocalRandom(int initialSeed)
        {
            _initSeed = initialSeed;
            _rnd = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref _initSeed)));
        }

        public ThreadLocalRandom() : this(Environment.TickCount)
        {
        }

        public int Next()
        {
            return _rnd.Value.Next();
        }

        public int Next(int maxValue)
        {
            return _rnd.Value.Next(maxValue);
        }

        public int Next(int minValue, int maxValue)
        {
            return _rnd.Value.Next(minValue, maxValue);
        }

        public double NextDouble()
        {
            return _rnd.Value.NextDouble();
        }

        private readonly ThreadLocal<Random> _rnd;
        private int _initSeed;
    }
}
