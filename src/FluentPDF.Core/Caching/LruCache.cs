namespace FluentPDF.Core.Caching;

/// <summary>
/// Thread-safe LRU (Least Recently Used) cache with automatic disposal of evicted items.
/// </summary>
/// <typeparam name="TKey">The type of keys in the cache.</typeparam>
/// <typeparam name="TValue">The type of values in the cache. Must implement IDisposable.</typeparam>
public sealed class LruCache<TKey, TValue> : IDisposable
    where TKey : notnull
    where TValue : IDisposable
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _cache;
    private readonly LinkedList<CacheItem> _lruList;
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the LruCache class with the specified capacity.
    /// </summary>
    /// <param name="capacity">The maximum number of items the cache can hold.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when capacity is less than 1.</exception>
    public LruCache(int capacity)
    {
        if (capacity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be at least 1.");
        }

        _capacity = capacity;
        _cache = new Dictionary<TKey, LinkedListNode<CacheItem>>(capacity);
        _lruList = new LinkedList<CacheItem>();
    }

    /// <summary>
    /// Gets the current number of items in the cache.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _cache.Count;
            }
        }
    }

    /// <summary>
    /// Gets the maximum capacity of the cache.
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// Attempts to retrieve a value from the cache and updates its position to most recently used.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">The value if found, otherwise default.</param>
    /// <returns>True if the key was found, otherwise false.</returns>
    public bool TryGet(TKey key, out TValue? value)
    {
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_cache.TryGetValue(key, out var node))
            {
                // Move to front (most recently used)
                _lruList.Remove(node);
                _lruList.AddFirst(node);

                value = node.Value.Value;
                return true;
            }

            value = default;
            return false;
        }
    }

    /// <summary>
    /// Adds or updates an item in the cache. If the cache is at capacity, the least recently used item is evicted and disposed.
    /// </summary>
    /// <param name="key">The key of the item to add.</param>
    /// <param name="value">The value to add.</param>
    public void Add(TKey key, TValue value)
    {
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            // If key already exists, remove old value
            if (_cache.TryGetValue(key, out var existingNode))
            {
                _lruList.Remove(existingNode);
                existingNode.Value.Value.Dispose();
                _cache.Remove(key);
            }

            // Evict LRU item if at capacity
            if (_cache.Count >= _capacity)
            {
                var lruNode = _lruList.Last;
                if (lruNode != null)
                {
                    _lruList.RemoveLast();
                    _cache.Remove(lruNode.Value.Key);
                    lruNode.Value.Value.Dispose();
                }
            }

            // Add new item at front (most recently used)
            var cacheItem = new CacheItem(key, value);
            var node = new LinkedListNode<CacheItem>(cacheItem);
            _lruList.AddFirst(node);
            _cache[key] = node;
        }
    }

    /// <summary>
    /// Removes a specific item from the cache and disposes it if found.
    /// </summary>
    /// <param name="key">The key of the item to remove.</param>
    /// <returns>True if the item was found and removed, otherwise false.</returns>
    public bool Remove(TKey key)
    {
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_cache.TryGetValue(key, out var node))
            {
                _lruList.Remove(node);
                _cache.Remove(key);
                node.Value.Value.Dispose();
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Removes all items from the cache and disposes them.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            foreach (var node in _lruList)
            {
                node.Value.Dispose();
            }

            _cache.Clear();
            _lruList.Clear();
        }
    }

    /// <summary>
    /// Disposes all cached items and releases resources.
    /// </summary>
    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            foreach (var node in _lruList)
            {
                node.Value.Dispose();
            }

            _cache.Clear();
            _lruList.Clear();
            _disposed = true;
        }
    }

    private readonly struct CacheItem
    {
        public TKey Key { get; }
        public TValue Value { get; }

        public CacheItem(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }
}
