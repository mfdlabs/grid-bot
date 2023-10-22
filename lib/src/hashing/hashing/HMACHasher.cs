namespace Hashing;

using System.Text;
using System.Security.Cryptography;

/// <summary>
/// Simple class for producing HMAC hashes
/// </summary>
public static class HMACHasher
{
    /// <summary>
    /// Build a HMAC hash string.
    /// </summary>
    /// <param name="stringToHash">The string to hash.</param>
    /// <param name="secretKey">The HMAC secret</param>
    /// <returns>The hashed string.</returns>
    public static byte[] BuildHMACSHA256HashString(string stringToHash, string secretKey)
    {
        using var hmacsha = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        return hmacsha.ComputeHash(Encoding.UTF8.GetBytes(stringToHash));
    }
}
