namespace Hashing;

using System.Text;
using System.Security.Cryptography;

/// <summary>
/// Simple class for producing SHA256 hashes
/// </summary>
public static class SHA256Hasher
{
    /// <summary>
    /// Build a SHA256 hash string.
    /// </summary>
    /// <param name="bytes">The bytes to hash.</param>
    /// <returns>The hashed string.</returns>
    public static string BuildSHA256HashString(byte[] bytes)
    {
        var hashBytes = GetSHA256Bytes(bytes);

        var stringBuilder = new StringBuilder();
        foreach (byte b in hashBytes)
            stringBuilder.Append(b.ToString("x2"));

        return stringBuilder.ToString();
    }

    /// <summary>
    /// Build a SHA256 hash string.
    /// </summary>
    /// <param name="stringToHash">The string to hash.</param>
    /// <returns>The hashed string.</returns>
    public static string BuildSHA256HashString(string stringToHash) 
        => BuildSHA256HashString(Encoding.UTF8.GetBytes(stringToHash));

    private static byte[] GetSHA256Bytes(byte[] originalBytes)
    {
        using var sha256Managed = new SHA256Managed();
        return sha256Managed.ComputeHash(originalBytes);
    }
}
