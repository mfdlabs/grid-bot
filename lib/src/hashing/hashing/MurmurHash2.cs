namespace Hashing;

using System;
using System.Runtime.InteropServices;

/// <summary>
/// MurmurHash is a non-cryptographic hash function suitable for general hash-based lookup.
/// 
/// MurmurHash2 yields a 32-bit or 64-bit value. It came in multiple variants, including some that allowed incremental hashing and aligned or neutral versions.
/// </summary>
[Obsolete("Use MurmurHash3 instead. See the discovered flaw here https://github.com/aappleby/smhasher/wiki/MurmurHash2Flaw")]
public class MurmurHash2
{
    private const uint _DefaultSeed = 0xc58f1a7b;

    private const uint _M = 0x5bd1e995;
    private const int _R = 24;

    [StructLayout(LayoutKind.Explicit)]
    private struct BytetoUInt32Converter
    {
        [FieldOffset(0)]
        public byte[] Bytes;

        [FieldOffset(0)]
        public readonly uint[] UInts;
    }

    /// <summary>
    /// Hash the given buffer with the default seed.
    /// </summary>
    /// <param name="data">The input buffer.</param>
    /// <returns>A hashed version of the input.</returns>
    public static uint Hash(byte[] data) => Hash(data, _DefaultSeed);

    /// <summary>
    /// Hash the given buffer with a seed.
    /// </summary>
    /// <param name="data">The input buffer.</param>
    /// <param name="seed">The seed to use for the hash.</param>
    /// <returns>A hashed version of the input.</returns>
    public static uint Hash(byte[] data, uint seed)
    {
        // Based on https://github.com/aappleby/smhasher/blob/master/src/MurmurHash2.cpp

        var len = data.Length;
        if (len == 0) return 0;

        var uints = new BytetoUInt32Converter { Bytes = data }.UInts;

        var h = seed ^ (uint)len;
        int idx = 0;
        while (len >= 4)
        {
            uint k = uints[idx++];

            k *= _M;
            k ^= k >> _R;
            k *= _M;

            h *= _M;
            h ^= k;

            len -= 4;
        }

        idx *= 4;
        switch (len)
        {
            case 1:
                h ^= data[idx];
                h *= _M;
                break;
            case 2:
                h ^= (ushort)(data[idx++] | data[idx] << 8);
                h *= _M;
                break;
            case 3:
                h ^= (ushort)(data[idx++] | data[idx++] << 8);
                h ^= (uint)data[idx] << 16;
                h *= _M;
                break;
        }


        h ^= h >> 13;
        h *= _M;
        h ^= h >> 15;

        return h;
    }

}
