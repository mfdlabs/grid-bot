namespace Hashing;

using System.Collections.Generic;

/// <summary>
/// Interface for a partitioned key gen
/// </summary>
public interface IPartitionedKeyGenerator
{
    /// <summary>
    /// Get the partition key.
    /// </summary>
    /// <param name="keyToBePartitioned">The key to be partitioned.</param>
    /// <returns>The partition key.</returns>
    string GetPartitionKey(string keyToBePartitioned);

    /// <summary>
    /// Get all partition keys.
    /// </summary>
    /// <returns>The partition keys.</returns>
    IReadOnlyCollection<string> GetAllPartitionKeys();
}