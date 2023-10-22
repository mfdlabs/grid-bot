namespace Redis;

using System;
using System.Text;
using System.Security.Cryptography;

/// <summary>
/// Hasher for Redis scripts.
/// </summary>
public static class LuaScriptHasher
{
    /// <summary>
    /// Get the SHA1 hash for the specified script.
    /// </summary>
    /// <param name="script">The script.</param>
    /// <returns>The hash bytes.</returns>
    public static byte[] GetScriptHash(string script)
    {
        using var sha1 = new SHA1Managed();

        return sha1.ComputeHash(Encoding.UTF8.GetBytes(script));
    }
}
