using System;

namespace Diagnostics
{
	internal class LongRandom
	{
        public LongRandom(Action<byte[]> nextBytes) => _next = nextBytes ?? throw new ArgumentNullException(nameof(nextBytes));

        public long NextLong(long minValue, long maxValue)
		{
			if (minValue < 0) throw new ArgumentOutOfRangeException(nameof(minValue));
			if (minValue > maxValue) throw new ArgumentOutOfRangeException(nameof(maxValue));

			var data = new byte[8];
			_next(data);
			var dataAsLong = BitConverter.ToInt64(data, 0);
			dataAsLong = dataAsLong % (maxValue - minValue) + minValue;
			if (dataAsLong == long.MinValue) return NextLong(minValue, maxValue);
			if (dataAsLong < 0) return Math.Abs(dataAsLong);
			return dataAsLong;
		}

		private readonly Action<byte[]> _next;
	}
}
