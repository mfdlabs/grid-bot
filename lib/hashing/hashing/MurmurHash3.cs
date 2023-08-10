namespace Hashing;

using System;
using System.Text;
using System.Runtime.CompilerServices;

/// <summary>
/// MurmurHash is a non-cryptographic hash function suitable for general hash-based lookup.
/// 
/// The current version is MurmurHash3, which yields a 32-bit or 128-bit hash value. When using 128-bits, 
/// the x86 and x64 versions do not produce the same values, as the algorithms are optimized for their respective platforms. 
/// MurmurHash3 was released alongside SMHasher—a hash function test suite.
/// </summary>
public class MurmurHash3
{
    private const uint _DefaultSeed = 0xb22a1385;

    private const uint _C1 = 0xcc9e2d51;
    private const uint _C2 = 0x1b873593;

    /// <summary>
    /// Hash the given buffer with the default seed.
    /// </summary>
    /// <param name="data">The input string.</param>
    /// <param name="seed">The seed to use for the hash.</param>
    /// <returns>A hashed version of the input.</returns>
    public static uint ComputeHash(string data, uint seed = _DefaultSeed)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        return ComputeHash(Encoding.UTF8.GetBytes(data), seed);
    }

    /// <summary>
    /// Hash the given buffer with a seed.
    /// </summary>
    /// <param name="data">The input buffer.</param>
    /// <param name="seed">The seed to use for the hash.</param>
    /// <returns>A hashed version of the input.</returns>
    public static uint ComputeHash(byte[] data, uint seed = _DefaultSeed)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        return ComputeHash(data, 0, data.Length, seed);
    }

    /// <summary>
    /// Hash the given buffer with a seed.
    /// </summary>
    /// <param name="data">The input buffer.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="length">The length</param>
    /// <param name="seed">The seed to use for the hash.</param>
    /// <returns>A hashed version of the input.</returns>
    public static uint ComputeHash(byte[] data, int offset, int length, uint seed = _DefaultSeed)
    {
        // Based on https://github.com/aappleby/smhasher/blob/master/src/MurmurHash3.cpp

        if (data == null) throw new ArgumentNullException(nameof(data));
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset), offset, "The offset cannot be negative");
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), length, "The length cannot be negative");
        if (offset + length > data.Length) throw new ArgumentException("offset + length must not exceed the length of the data array");

        uint h1 = seed;

        //----------
        // body

        if (length > 3)
        {
            int numBlocks = length >> 2;
            do
            {
                uint k1 = BitConverter.ToUInt32(data, offset);

                offset += 4;

                k1 *= _C1;
                k1 = Rotl32(k1, 15);
                k1 *= _C2;

                h1 ^= k1;
                h1 = Rotl32(k1, 13);
                h1 = h1 * 5 + 0xe6546b64;
            }
            while (--numBlocks > 0);
        }

        //----------
        // tail

        if ((length & 3) != 0)
        {
            int len = length & 3;
            uint k1 = 0;
            do
            {
                k1 <<= 8;
                k1 |= data[offset + len - 1];
            }
            while (--len > 0);

            k1 *= _C1;
            k1 = Rotl32(k1, 15);
            k1 *= _C2;
            h1 ^= k1;
        }

        //----------
        // finalization

        h1 ^= (uint)length;

        h1 ^= h1 >> 16;
        h1 *= 0x85ebca6b;
        h1 ^= h1 >> 13;
        h1 *= 0xc2b2ae35;
        h1 ^= h1 >> 16;

        return h1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Rotl32(uint value, int shift)
    {
        return (value << shift) | (value >> (32 - shift));
    }
}
