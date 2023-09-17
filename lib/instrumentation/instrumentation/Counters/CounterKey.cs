using System;
using System.Diagnostics;
using Text.Extensions;

namespace Instrumentation
{
    [DebuggerDisplay("Category={Category}, Name={Name}, Instance={Instance}")]
    public struct CounterKey : IEquatable<CounterKey>
    {
        public string Category { get; }
        public string Name { get; }
        public string Instance { get; }

        public CounterKey(string category, string name, string instance)
        {
            if (category.IsNullOrEmpty()) throw new ArgumentException(nameof(category));
            if (name.IsNullOrEmpty()) throw new ArgumentException(nameof(name));
            Category = category;
            Name = name;
            Instance = instance;
            _Key = string.Join("\t", category, name, instance);
        }

        public bool Equals(CounterKey other) => string.Equals(_Key, other._Key);
        public override bool Equals(object obj)
        {
            if (obj is CounterKey other) return Equals(other);
            return false;
        }
        public override int GetHashCode() => _Key.GetHashCode();

        private readonly string _Key;
    }
}
