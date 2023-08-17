namespace Hashing;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

/// <inheritdoc cref="IPartitionedKeyGenerator"/>
public class PartitionedKeyGenerator : IPartitionedKeyGenerator
{
    private readonly ConsistentHash<PartitionWrapper> _ConsistentHash;
    private readonly IReadOnlyCollection<string> _PartitionKeys;

    /// <summary>
    /// Construct a new instance of <see cref="PartitionedKeyGenerator"/>
    /// </summary>
    /// <param name="numberOfPartitions">The number of partitions</param>
    /// <param name="partitionPrefix">The prefix for partitions.</param>
    /// <exception cref="ArgumentException">
    /// - <paramref name="numberOfPartitions"/>
    /// - <paramref name="partitionPrefix"/>
    /// </exception>
    public PartitionedKeyGenerator(int numberOfPartitions, string partitionPrefix)
    {
        if (numberOfPartitions <= 0) throw new ArgumentException(nameof(numberOfPartitions));
        if (string.IsNullOrEmpty(partitionPrefix)) throw new ArgumentException(nameof(partitionPrefix));

        var nodes = new List<PartitionWrapper>(numberOfPartitions);
        for (int i = 1; i <= numberOfPartitions; i++)
            nodes.Add(new PartitionWrapper(string.Format("{0}_{1}", partitionPrefix, i)));

        _PartitionKeys = new ReadOnlyCollection<string>((from p in nodes
                                                         select p.PartitionKey).ToList());

        _ConsistentHash = new ConsistentHash<PartitionWrapper>();
        _ConsistentHash.Init(nodes);
    }

    /// <inheritdoc cref="IPartitionedKeyGenerator.GetPartitionKey(string)"/>
    public string GetPartitionKey(string keyToBePartitioned) => _ConsistentHash.GetNode(keyToBePartitioned).PartitionKey;

    /// <inheritdoc cref="IPartitionedKeyGenerator.GetAllPartitionKeys"/>
    public IReadOnlyCollection<string> GetAllPartitionKeys() => _PartitionKeys;
}

/// <summary>
/// Class for wrapping partition keys.
/// </summary>
public class PartitionWrapper
{
    private readonly int _Hash;

    /// <summary>
    /// The partition key of this wrapper.
    /// </summary>
    public string PartitionKey { get; }

    /// <summary>
    /// Construct a new instance of <see cref="PartitionWrapper"/>
    /// </summary>
    /// <param name="partitionKey">The partition key</param>
    public PartitionWrapper(string partitionKey)
    {
        PartitionKey = partitionKey;

        _Hash = (int)MurmurHash2.Hash(Encoding.ASCII.GetBytes(PartitionKey));
    }

    /// <inheritdoc cref="object.GetHashCode"/>
    public override int GetHashCode() => _Hash;
}
