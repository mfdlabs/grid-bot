namespace Random;

using System;
using System.Security.Cryptography;

/// <summary>
/// <see cref="IRandom"/> using <see cref="RNGCryptoServiceProvider"/>
/// </summary>
public class RngCryptoServiceRandom : IRandom
{
    private readonly LongRandom _longRng;
    private readonly RandomNumberGenerator _rng = new RNGCryptoServiceProvider();

    /// <summary>
    /// Construct a new instance of <see cref="RngCryptoServiceRandom"/>
    /// </summary>
    public RngCryptoServiceRandom() => _longRng = new LongRandom(NextBytes);

    /// <inheritdoc cref="IRandom.Next()"/>
    public int Next()
    {
        var data = new byte[4];
        _rng.GetBytes(data);

        return BitConverter.ToInt32(data, 0) & int.MaxValue;
    }

    /// <inheritdoc cref="IRandom.Next(int)"/>
    public int Next(int maxValue) => Next(0, maxValue);

    /// <inheritdoc cref="IRandom.Next(int, int)"/>
    public int Next(int minValue, int maxValue)
    {
        if (minValue > maxValue) throw new ArgumentOutOfRangeException();

        return (int)((long)Math.Floor(minValue + (maxValue - minValue) * NextDouble()));
    }

    /// <inheritdoc cref="IRandom.NextBytes(byte[])"/>
    public void NextBytes(byte[] buffer) => _rng.GetBytes(buffer);

    /// <inheritdoc cref="IRandom.NextDouble()"/>
    public double NextDouble()
    {
        var data = new byte[4];
        _rng.GetBytes(data);

        return BitConverter.ToUInt32(data, 0) / float.MaxValue;
    }

    /// <inheritdoc cref="IRandom.NextLong()"/>
    public long NextLong() => NextLong(long.MaxValue);

    /// <inheritdoc cref="IRandom.NextLong(long)"/>
    public long NextLong(long maxValue)
    {
        if (maxValue < 0) throw new ArgumentOutOfRangeException(nameof(maxValue));

        return NextLong(0, maxValue);
    }

    /// <inheritdoc cref="IRandom.NextLong(long, long)"/>
    public long NextLong(long minValue, long maxValue) => _longRng.NextLong(minValue, maxValue);

}
