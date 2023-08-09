namespace Grid;

using System;
using System.Security.Cryptography;

/// <summary>
/// Provides a factory for spitting out the default random number generator.
/// </summary>
public static class RandomFactory
{
    private static readonly RandomNumberGenerator _CryptoSeedGenerator = new RNGCryptoServiceProvider();
    private static readonly IRandom _DefaultRandom = new ThreadLocalRandom(GetCryptoSeed());

    /// <summary>
    /// Gets the default random number generator.
    /// </summary>
    /// <returns>The default random number generator.</returns>
    public static IRandom GetDefaultRandom() => _DefaultRandom;

    private static int GetCryptoSeed()
    {
        var data = new byte[4];
        _CryptoSeedGenerator.GetBytes(data);
        return BitConverter.ToInt32(data, 0);
    }
}
