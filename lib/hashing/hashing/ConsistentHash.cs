namespace Hashing;

using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// Represents a consistent hash.
/// </summary>
/// <typeparam name="TNode">The type of the nodes within the hash.</typeparam>
public class ConsistentHash<TNode>
{
    private int[] _AyKeys;
    private int _Replicate = 100;

    /// <summary>
    /// The Hash circle.
    /// </summary>
    protected SortedDictionary<int, TNode> _Circle = new();

    /// <summary>
    /// Initializes a new consistent hash.
    /// </summary>
    /// <param name="nodes">The nodes.</param>
    public void Init(IEnumerable<TNode> nodes) => Init(nodes, _Replicate);

    /// <summary>
    /// Initializes a new consistent hash with replicas.
    /// </summary>
    /// <param name="nodes">The nodes.</param>
    /// <param name="replicate">The number of replicas.</param>
    public void Init(IEnumerable<TNode> nodes, int replicate)
    {
        _Replicate = replicate;
        foreach (var node in nodes) Add(node, false);
        _AyKeys = _Circle.Keys.ToArray();
    }

    /// <summary>
    /// Adds a node to the hash automatically updating the key array.
    /// </summary>
    /// <param name="node">The node.</param>
    public void Add(TNode node) => Add(node, true);

    /// <summary>
    /// Removes a node from the hash.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <exception cref="Exception">Can not remove a node that not added</exception>
    public void Remove(TNode node)
    {
        for (int i = 0; i < _Replicate; i++)
            if (!_Circle.Remove(BetterHash(node.GetHashCode().ToString() + i)))
                throw new Exception("can not remove a node that not added");

        _AyKeys = _Circle.Keys.ToArray();
    }

    /// <summary>
    /// Get a node from the hash with the given key.
    /// </summary>
    /// <param name="key">The node key.</param>
    /// <returns>The requested node or null.</returns>
    public TNode GetNode(string key) => _Circle[_AyKeys[First_ge(_AyKeys, BetterHash(key))]];

    /// <summary>
    /// Generates a new MurmurHash2 from the given key.
    /// </summary>
    /// <param name="key"></param>
    /// <returns>The hash as an integer.</returns>
    public static int BetterHash(string key) => (int)MurmurHash2.Hash(Encoding.ASCII.GetBytes(key));

    ////////////////////////////////////////////////////////////////////////////////////////
    /// PRIVATE METHODS

    private void Add(TNode node, bool updateKeyArray)
    {
        for (int i = 0; i < _Replicate; i++)
            _Circle[BetterHash(node.GetHashCode().ToString() + i)] = node;

        if (updateKeyArray) _AyKeys = _Circle.Keys.ToArray();
    }

    private TNode GetNode_slow(string key)
    {
        int hash = BetterHash(key);
        if (_Circle.ContainsKey(hash))
            return _Circle[hash];

        int h1 = _Circle.Keys.FirstOrDefault(h => h >= hash);
        if (h1 == 0)
            h1 = _AyKeys[0];

        return _Circle[h1];
    }

    private int First_ge(int[] ay, int val)
    {
        int idx = 0;
        int ayiter = ay.Length - 1;
        if (ay[ayiter] < val || ay[0] > val) return 0;
        while (ayiter - idx > 1)
        {
            int num3 = (ayiter + idx) / 2;
            if (ay[num3] >= val) ayiter = num3;
            else idx = num3;
        }
        if (ay[idx] > val || ay[ayiter] < val)
            throw new Exception("should not happen");

        return ayiter;
    }
}
