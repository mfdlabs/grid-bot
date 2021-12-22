using System;
using System.Threading;

namespace MFDLabs.Threading
{
    public class ThreadLocalRandom
    {
        public ThreadLocalRandom(int initialSeed)
            => _rnd = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref initialSeed)));

        public ThreadLocalRandom() 
            : this(Environment.TickCount)
        {
        }

        public int Next() => _rnd.Value.Next();
        public int Next(int maxValue) => _rnd.Value.Next(maxValue);
        public int Next(int minValue, int maxValue) => _rnd.Value.Next(minValue, maxValue);
        public double NextDouble() => _rnd.Value.NextDouble();

        private readonly ThreadLocal<Random> _rnd;
    }
}
