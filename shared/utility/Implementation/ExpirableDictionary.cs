namespace Grid.Bot.Utility;

using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;

/// <summary>
/// Represents the policy for an expirable collection.
/// </summary>
public enum ExpirationPolicy
{
    /// <summary>
    /// Never renew the collection.
    /// </summary>
    NeverRenew,

    /// <summary>
    /// Renew on each read.
    /// </summary>
    RenewOnRead
}

/// <summary>
/// Represents the reason for removal.
/// </summary>
public enum RemovalReason
{
    /// <summary>
    /// The element expired.
    /// </summary>
    Expired,

    /// <summary>
    /// The element was explicitly removed.
    /// </summary>
    ExplicitlyRemoved
}

/// <summary>
/// Represents a dictionary with expirable entrys.
/// </summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <typeparam name="TValue">The type of the value.</typeparam>
public class ExpirableDictionary<TKey, TValue> : IDisposable
    where TValue : class
{
    private class ExpirableValue
    {
        private DateTime _Expiration;
        private DateTime _Updated;

        public TValue Value { get; }

        public ExpirableValue(TValue value, TimeSpan timeToLive)
        {
            Value = value;

            ExtendExpiration(timeToLive);
        }

        public bool IsExpired(DateTime now) => now >= _Expiration;

        public void ExtendExpiration(TimeSpan timeToLive)
        {
            _Updated = DateTime.UtcNow;
            _Expiration = _Updated + timeToLive;
        }
    }

    private readonly TimeSpan _TraversalInterval;
    private readonly ExpirationPolicy _ExpirationPolicy;
    private readonly Func<TimeSpan> _TimeToLiveGetter;

    private ConcurrentDictionary<TKey, ExpirableValue> _Entries = new();
    private Timer _Timer;
    private bool _Disposed;

    /// <summary>
    /// The event invoked pre traversal of the dictionary.
    /// </summary>
    public event Action PreTraversal;

    /// <summary>
    /// The event invoked when a traversal is complete.
    /// </summary>
    public event Action TraversalComplete;

    /// <summary>
    /// Event invoked when an entry is removed.
    /// </summary>
    public event Action<TValue, RemovalReason> EntryRemoved;

    /// <summary>
    /// Event invoked when an entry is traversed.
    /// </summary>
    public event Action<TValue, DateTime> EntryTraversed;

    /// <summary>
    /// Event invoked when a exception occurs.
    /// </summary>
    public event Action<Exception> ExceptionOccurred;

    /// <summary>
    /// Construct a new instance of <see cref="ExpirableDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="entryTimeToLive">The time to live for each entry.</param>
    public ExpirableDictionary(TimeSpan entryTimeToLive)
        : this(entryTimeToLive, TimeSpan.FromSeconds(1))
    {
    }

    /// <summary>
    /// Construct a new instance of <see cref="ExpirableDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="entryTimeToLive">The time to live for each entry.</param>
    /// <param name="traversalInterval">The interval to traverse the dictionary.</param>
    public ExpirableDictionary(TimeSpan entryTimeToLive, TimeSpan traversalInterval)
        : this(() => entryTimeToLive, traversalInterval, ExpirationPolicy.RenewOnRead)
    {
    }

    /// <summary>
    /// Construct a new instance of <see cref="ExpirableDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="entryTimeToLiveGetter">The time to live for each entry.</param>
    /// <param name="expirationPolicy">The <see cref="ExpirationPolicy"/></param>
    public ExpirableDictionary(Func<TimeSpan> entryTimeToLiveGetter, ExpirationPolicy expirationPolicy)
        : this(entryTimeToLiveGetter, TimeSpan.FromSeconds(1.0), expirationPolicy)
    {
    }

    /// <summary>
    /// Construct a new instance of <see cref="ExpirableDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="entryTimeToLiveGetter">The time to live for each entry.</param>
    /// <param name="traversalInterval">The interval to traverse the dictionary.</param>
    /// <param name="expirationPolicy">The <see cref="ExpirationPolicy"/></param>
    /// <exception cref="ArgumentNullException"><paramref name="entryTimeToLiveGetter"/> cannot be null.</exception>
    public ExpirableDictionary(Func<TimeSpan> entryTimeToLiveGetter, TimeSpan traversalInterval, ExpirationPolicy expirationPolicy)
    {
        _TraversalInterval = traversalInterval;
        _ExpirationPolicy = expirationPolicy;
        _TimeToLiveGetter = entryTimeToLiveGetter ?? throw new ArgumentNullException(nameof(entryTimeToLiveGetter));

        _Timer = new Timer(TraverseAndPurge, null, traversalInterval, traversalInterval);
    }

    /// <summary>
    /// Clear the dictionary.
    /// </summary>
    public void Clear() => _Entries = new();

    /// <summary>
    /// Gets or adds to the dictionary.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="valueFactory">The function that returns the new value.</param>
    /// <returns>The value.</returns>
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
        var expirableValue = _Entries.GetOrAdd(key, s => new(valueFactory(key), _TimeToLiveGetter()));

        if (_ExpirationPolicy == ExpirationPolicy.RenewOnRead)
            expirableValue.ExtendExpiration(_TimeToLiveGetter());

        return expirableValue.Value;
    }

    /// <summary>
    /// Set the value of the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public void Set(TKey key, TValue value) => _Entries[key] = new(value, _TimeToLiveGetter());

    /// <summary>
    /// Remove the specified value from the dictionary.
    /// </summary>
    /// <param name="key"></param>
    /// <returns>The removed value.</returns>
    public TValue Remove(TKey key)
    {
        _Entries.TryRemove(key, out var removed);

        if (removed != null)
        {
            EntryRemoved?.Invoke(removed.Value, RemovalReason.ExplicitlyRemoved);

            return removed.Value;
        }

        return default(TValue);
    }

    /// <summary>
    /// Get the value of the dictionary.
    /// </summary>
    /// <returns>The values of the dictionary.</returns>
    public IEnumerable<TValue> GetValues()
    {
        var now = DateTime.UtcNow;
        foreach (var kvp in _Entries)
            if (!kvp.Value.IsExpired(now))
                yield return kvp.Value.Value;
    }

    /// <summary>
    /// Get the keys of the dictionary.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<TKey> GetKeys()
    {
        var now = DateTime.UtcNow;
        foreach (var kvp in _Entries)
            if (!kvp.Value.IsExpired(now))
                yield return kvp.Key;
    }

    /// <summary>
    /// Get the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The value.</returns>
    public TValue Get(TKey key)
    {
        if (_Entries.TryGetValue(key, out var value))
        {
            if (_ExpirationPolicy == ExpirationPolicy.RenewOnRead)
                value.ExtendExpiration(_TimeToLiveGetter());

            return value.Value;
        }

        return default(TValue);
    }

    /// <summary>
    /// Check if the key exists.  Will not extend expiration.
    /// </summary>
    /// <param name="key"></param>
    /// <returns>True if the key exists</returns>
    public bool ContainsKey(TKey key) => _Entries.ContainsKey(key);

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose of the counter.
    /// </summary>
    /// <param name="disposing">Is disposing?</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_Disposed) return;

        if (disposing)
            _Timer?.Dispose();

        _Timer = null;
        _Disposed = true;
    }

    private void TraverseAndPurge(object timer)
    {
        _Timer.Change(-1, -1);

        try
        {
            PreTraversal?.Invoke();

            var now = DateTime.UtcNow;
            foreach (var kvp in _Entries)
            {
                var value = kvp.Value;

                EntryTraversed?.Invoke(value.Value, now);

                if (value.IsExpired(now))
                {
                    _Entries.TryRemove(kvp.Key, out var _);

                    EntryRemoved?.Invoke(value.Value, RemovalReason.Expired);
                }
            }

            TraversalComplete?.Invoke();
        }
        catch (Exception ex)
        {
            if (ExceptionOccurred != null)
            {
                try
                {
                    ExceptionOccurred(ex);
                }
                catch
                {
                }
            }
        }

        _Timer.Change(_TraversalInterval, _TraversalInterval);
    }
}
