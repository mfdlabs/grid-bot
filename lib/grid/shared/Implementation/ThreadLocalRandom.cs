namespace Grid;

using System;
using System.Threading;

/// <summary>
/// Provides a thread-local random number generator.
/// </summary>
internal class ThreadLocalRandom : IRandom
{
    private readonly ThreadLocal<Random> _rng;
    private readonly LongRandom _longRng;
    private int _seed;

    /// <summary>
    /// Construct a new instance of <see cref="ThreadLocalRandom"/>.
    /// </summary>
    /// <param name="initialSeed">The inital seed to use.</param>
    public ThreadLocalRandom(int initialSeed)
    {
        _seed = initialSeed;
        _rng = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref _seed)));
        _longRng = new LongRandom(NextBytes);
    }

    /// <summary>
    /// Construct a new instance of <see cref="ThreadLocalRandom"/>.
    /// </summary>
    public ThreadLocalRandom()
        : this(Environment.TickCount)
    { }

    /// <inheritdoc cref="IRandom.Next()"/>
    public int Next() => _rng.Value.Next();

    /// <inheritdoc cref="IRandom.Next(int)"/>
    public int Next(int maxValue) => _rng.Value.Next(maxValue);

    /// <inheritdoc cref="IRandom.Next(int, int)"/>
    public int Next(int minValue, int maxValue) => _rng.Value.Next(minValue, maxValue);

    /// <inheritdoc cref="IRandom.NextDouble"/>
    public double NextDouble() => _rng.Value.NextDouble();

    /// <inheritdoc cref="IRandom.NextBytes(byte[])"/>
    public void NextBytes(byte[] buffer) => _rng.Value.NextBytes(buffer);

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
