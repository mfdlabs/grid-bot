namespace Grid.Bot.Extensions;

using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Extension methods for Dictionary.
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    /// Merges the current dictionary with others, left to right.
    /// If a key exists in multiple dictionaries, the value from the last one will be used.
    /// </summary>
    /// <typeparam name="T">The type of the dictionary.</typeparam>
    /// <typeparam name="K">The type of the key.</typeparam>
    /// <typeparam name="V">The type of the value.</typeparam>
    /// <param name="me">The current dictionary.</param>
    /// <param name="others">The dictionaries to merge with.</param>
    /// <returns>A new dictionary containing the merged key-value pairs.</returns>
    public static T MergeLeft<T, K, V>(this T me, params IDictionary<K, V>[] others)
        where T : IDictionary<K, V>, new()
    {
        var newMap = new T();

        foreach (IDictionary<K, V> src in new List<IDictionary<K, V>> { me }.Concat(others))
        {
            // ^-- echk. Not quite there type-system.
            foreach (KeyValuePair<K, V> p in src)
            {
                newMap[p.Key] = p.Value;
            }
        }

        return newMap;
    }
}