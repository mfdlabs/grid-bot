namespace Grid;

using System;

/// <summary>
/// Internal random for computing random numbers based on 64-bit integers.
/// </summary>
internal class LongRandom
{
    private readonly Action<byte[]> _next;

    /// <summary>
    /// Construct a new instance of <see cref="LongRandom"/>.
    /// </summary>
    /// <param name="nextBytes">A function that returns a random byte array.</param>
    /// <exception cref="ArgumentNullException"><paramref name="nextBytes"/> cannot be null.</exception>
    public LongRandom(Action<byte[]> nextBytes) => _next = nextBytes ?? throw new ArgumentNullException(nameof(nextBytes));

    /// <summary>
    /// Compute a random 64-bit integer withing the specified range.
    /// </summary>
    /// <param name="minValue">The minimum value.</param>
    /// <param name="maxValue">The maximum value.</param>
    /// <returns>A random 64-bit integer.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// - <paramref name="minValue"/> cannot be less than 0.
    /// - <paramref name="minValue"/> cannot be greater than <paramref name="maxValue"/>.
    /// </exception>
    public long NextLong(long minValue, long maxValue)
    {
        if (minValue < 0) throw new ArgumentOutOfRangeException(nameof(minValue));
        if (minValue > maxValue) throw new ArgumentOutOfRangeException(nameof(maxValue));

        var data = new byte[8];

        _next(data);

        var dataAsLong = BitConverter.ToInt64(data, 0);
        
        dataAsLong = dataAsLong % (maxValue - minValue) + minValue;
        
        if (dataAsLong == long.MinValue) 
            return NextLong(minValue, maxValue);
        
        if (dataAsLong < 0) 
            return Math.Abs(dataAsLong);

        return dataAsLong;
    }
}
