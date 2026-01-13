namespace Grid.Bot.Extensions;

using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Extension methods for Dictionary.
/// </summary>
public static class DictionaryExtensions
{
    /// <param name="me">The current dictionary.</param>
    /// <typeparam name="T">The type of the dictionary.</typeparam>
    /// <typeparam name="K">The type of the key.</typeparam>
    /// <typeparam name="V">The type of the value.</typeparam>
    extension<T, K, V>(T me) 
        where T : IDictionary<K, V>, new()
    {
        /// <summary>
        /// Merges the current dictionary with others, left to right.
        /// If a key exists in multiple dictionaries, the value from the last one will be used.
        /// </summary>
        /// <param name="others">The dictionaries to merge with.</param>
        /// <returns>A new dictionary containing the merged key-value pairs.</returns>
        public T MergeLeft(params IDictionary<K, V>[] others)
        {
            var newMap = new T();

            foreach (var src in new List<IDictionary<K, V>> { me }.Concat(others))
            {
                // ^-- echk. Not quite there type-system.
                foreach (var p in src)
                {
                    newMap[p.Key] = p.Value;
                }
            }

            return newMap;
        }
    }
}
