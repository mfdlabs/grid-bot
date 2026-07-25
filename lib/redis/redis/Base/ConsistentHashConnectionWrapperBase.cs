namespace Redis;

using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using StackExchange.Redis;

using Hashing;

internal abstract class ConsistentHashConnectionWrapperBase
{
    private readonly int _Hash;

    public abstract IDatabase Database { get; }
    public abstract ISubscriber Subscriber { get; }
    public abstract IServer Server { get; }

    internal ConsistentHashConnectionWrapperBase(ConfigurationOptions configuration)
    {
        _Hash = (int)MurmurHash3.ComputeHash(Encoding.ASCII.GetBytes(string.Join("_", configuration.EndPoints)));
    }

    public override int GetHashCode() => _Hash;

    internal static IDictionary<IDatabase, IReadOnlyCollection<string>> GetDatabasesByConsistentHashingAlgorithm(IEnumerable<string> keys, ConsistentHash<ConsistentHashConnectionWrapperBase> wrapperProvider)
    {
        var redisConnectionToBucketMapping = new Dictionary<ConsistentHashConnectionWrapperBase, ICollection<string>>();
        
        foreach (var key in keys)
        {
            var node = wrapperProvider.GetNode(key);

            ICollection<string> newKeys;
            if (!redisConnectionToBucketMapping.ContainsKey(node))
            {
                newKeys = new List<string>();
                redisConnectionToBucketMapping.Add(node, newKeys);
            }
            else
                newKeys = redisConnectionToBucketMapping[node];

            newKeys.Add(key);
        }

        return (IDictionary<IDatabase, IReadOnlyCollection<String>>)redisConnectionToBucketMapping.ToDictionary(
            kvp => kvp.Key.Database,
            kvp => redisConnectionToBucketMapping[kvp.Key].ToList<string>().AsReadOnly()
        );
    }
}
