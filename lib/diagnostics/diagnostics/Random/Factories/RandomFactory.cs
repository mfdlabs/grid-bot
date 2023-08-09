using System;
using System.Security.Cryptography;
using System.Threading;

namespace MFDLabs.Diagnostics
{
	public static class RandomFactory 
	{
		public class ThreadLocalRandom : IRandom
		{
			public ThreadLocalRandom(int initialSeed)
			{
				_seed = initialSeed;
				_rng = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref _seed)));
				_longRng = new LongRandom(new Action<byte[]>(NextBytes));
			}

			public ThreadLocalRandom()
				: this(Environment.TickCount)
			{}

            public int Next() => _rng.Value.Next();
            public int Next(int maxValue) => _rng.Value.Next(maxValue);
            public int Next(int minValue, int maxValue) => _rng.Value.Next(minValue, maxValue);
            public double NextDouble() => _rng.Value.NextDouble();
            public void NextBytes(byte[] buffer) => _rng.Value.NextBytes(buffer);
            public long NextLong() => NextLong(long.MaxValue);
            public long NextLong(long maxValue)
			{
				if (maxValue < 0) throw new ArgumentOutOfRangeException(nameof(maxValue));
				return NextLong(0, maxValue);
			}
            public long NextLong(long minValue, long maxValue) => _longRng.NextLong(minValue, maxValue);

            private readonly ThreadLocal<Random> _rng;
			private readonly LongRandom _longRng;
			private int _seed;
		}

		public static IRandom GetDefaultRandom() => _DefaultRandom;

        private static int GetCryptoSeed()
		{
			var data = new byte[4];
			_CryptoSeedGenerator.GetBytes(data);
			return BitConverter.ToInt32(data, 0);
		}

		private static readonly RandomNumberGenerator _CryptoSeedGenerator = new RNGCryptoServiceProvider();
		private static readonly IRandom _DefaultRandom = new ThreadLocalRandom(GetCryptoSeed());
	}
}
