namespace Random;

using System;
using System.Security.Cryptography;

/// <summary>
/// Provides a factory for spitting out the default random number generator.
/// </summary>
public static class RandomFactory
{
    private static readonly RandomNumberGenerator _cryptoSeedGenerator = new RNGCryptoServiceProvider();
    private static readonly IRandom _defaultRandom = new ThreadLocalRandom(GetCryptoSeed());

    /// <summary>
    /// Gets the default random number generator.
    /// </summary>
    /// <returns>The default random number generator.</returns>
    public static IRandom GetDefaultRandom() => _defaultRandom;

    private static int GetCryptoSeed()
    {
        var data = new byte[4];
        _cryptoSeedGenerator.GetBytes(data);
        return BitConverter.ToInt32(data, 0);
    }
}
