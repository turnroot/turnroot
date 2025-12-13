using System;
using System.Collections.Generic;

namespace Turnroot.Utilities
{
    /// <summary>
    /// Generic cache manager to reduce code duplication across caching implementations.
    /// Handles cache invalidation and lazy loading patterns.
    /// </summary>
    /// <typeparam name="TKey">The type of the cache key.</typeparam>
    /// <typeparam name="TValue">The type of the cached value.</typeparam>
    public class CacheManager<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _cache = new();
        private readonly Func<TKey, TValue> _valueFactory;
        private bool _isDirty = true;

        /// <summary>
        /// Creates a new cache manager with a factory function for generating values.
        /// </summary>
        /// <param name="valueFactory">Function to create values when not in cache.</param>
        public CacheManager(Func<TKey, TValue> valueFactory)
        {
            _valueFactory = valueFactory ?? throw new ArgumentNullException(nameof(valueFactory));
        }

        /// <summary>
        /// Gets a value from the cache or creates it if not present.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns>The cached or newly created value.</returns>
        public TValue Get(TKey key)
        {
            if (_cache.TryGetValue(key, out var value))
            {
                return value;
            }

            value = _valueFactory(key);
            _cache[key] = value;
            return value;
        }

        /// <summary>
        /// Gets a value from the cache or creates it using the provided factory if not present.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="factory">Function to create the value if not in cache.</param>
        /// <returns>The cached or newly created value.</returns>
        public TValue GetOrAdd(TKey key, Func<TValue> factory)
        {
            if (_cache.TryGetValue(key, out var value))
            {
                return value;
            }

            value = factory();
            _cache[key] = value;
            return value;
        }

        /// <summary>
        /// Invalidates the entire cache, forcing regeneration on next access.
        /// </summary>
        public void Invalidate()
        {
            _cache.Clear();
            _isDirty = true;
        }

        /// <summary>
        /// Invalidates a specific cache entry.
        /// </summary>
        /// <param name="key">The key to invalidate.</param>
        public void Invalidate(TKey key) => _cache.Remove(key);

        /// <summary>
        /// Checks if the cache contains a specific key.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key exists in cache.</returns>
        public bool Contains(TKey key) => _cache.ContainsKey(key);

        /// <summary>
        /// Gets the number of items in the cache.
        /// </summary>
        public int Count => _cache.Count;

        /// <summary>
        /// Indicates if the cache is marked as dirty.
        /// </summary>
        public bool IsDirty => _isDirty;

        /// <summary>
        /// Marks the cache as clean.
        /// </summary>
        public void MarkClean() => _isDirty = false;
    }

    /// <summary>
    /// Simple cache manager for single-value caching with invalidation.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    public class SingleValueCache<T>
    {
        private T _cachedValue;
        private bool _isDirty = true;
        private readonly Func<T> _valueFactory;

        /// <summary>
        /// Creates a new single-value cache with a factory function.
        /// </summary>
        /// <param name="valueFactory">Function to create the value when cache is invalid.</param>
        public SingleValueCache(Func<T> valueFactory)
        {
            _valueFactory = valueFactory ?? throw new ArgumentNullException(nameof(valueFactory));
        }

        /// <summary>
        /// Creates a new single-value cache without a factory function.
        /// Use GetOrCompute() to provide factory function on each access.
        /// </summary>
        public SingleValueCache()
        {
            _valueFactory = null;
        }

        /// <summary>
        /// Gets the cached value or creates it using the provided factory if the cache is dirty.
        /// </summary>
        public T GetOrCompute(Func<T> factory)
        {
            if (_isDirty)
            {
                _cachedValue = factory();
                _isDirty = false;
            }
            return _cachedValue;
        }

        /// <summary>
        /// Gets the cached value or creates it if the cache is dirty.
        /// Requires factory function to be provided in constructor.
        /// </summary>
        public T Value
        {
            get
            {
                if (_valueFactory == null)
                {
                    throw new InvalidOperationException(
                        "Value property requires factory function in constructor. Use GetOrCompute() instead."
                    );
                }

                if (_isDirty)
                {
                    _cachedValue = _valueFactory();
                    _isDirty = false;
                }
                return _cachedValue;
            }
        }

        /// <summary>
        /// Invalidates the cache, forcing regeneration on next access.
        /// </summary>
        public void Invalidate() => _isDirty = true;

        /// <summary>
        /// Indicates if the cache is marked as dirty.
        /// </summary>
        public bool IsDirty => _isDirty;
    }
}
