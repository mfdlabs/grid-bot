namespace Hashing;

using System.Collections.Generic;


/// <summary>
/// Represents a consistent hash V2.
/// </summary>
/// <typeparam name="TNode">The type of the nodes within the hash.</typeparam>
public class ConsistentHashV2<TNode> : ConsistentHash<TNode>
{
    /// <summary>
    /// The Nodes of this consistent hash.
    /// </summary>
    public IReadOnlyCollection<TNode> Nodes { get; private set; }

    /// <summary>
    /// Init the hash with the specified nodes.
    /// </summary>
    /// <param name="nodes">The nodes.</param>
    public void Init(IReadOnlyCollection<TNode> nodes)
    {
        Nodes = nodes;
        _Circle = new SortedDictionary<int, TNode>();

        base.Init(nodes);
    }
}
