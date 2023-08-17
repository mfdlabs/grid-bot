using System;
using System.Security.Cryptography;

namespace Diagnostics
{
	public class RngCryptoServiceRandom : IRandom
	{
        public RngCryptoServiceRandom() => _longRng = new LongRandom(new Action<byte[]>(NextBytes));

        public int Next()
		{
			var data = new byte[4];
			_rng.GetBytes(data);
			return BitConverter.ToInt32(data, 0) & int.MaxValue;
		}
        public int Next(int maxValue) => Next(0, maxValue);
        public int Next(int minValue, int maxValue)
		{
			if (minValue > maxValue) throw new ArgumentOutOfRangeException();
			return (int)((long)Math.Floor(minValue + (maxValue - minValue) * NextDouble()));
		}
        public void NextBytes(byte[] buffer) => _rng.GetBytes(buffer);
        public double NextDouble()
		{
			var data = new byte[4];
			_rng.GetBytes(data);
			return BitConverter.ToUInt32(data, 0) / float.MaxValue;
		}
        public long NextLong() => NextLong(long.MaxValue);
        public long NextLong(long maxValue)
		{
			if (maxValue < 0) throw new ArgumentOutOfRangeException(nameof(maxValue));
			return NextLong(0, maxValue);
		}
        public long NextLong(long minValue, long maxValue) => _longRng.NextLong(minValue, maxValue);

        private readonly RandomNumberGenerator _rng = new RNGCryptoServiceProvider();
		private readonly LongRandom _longRng;
	}
}
