using System.Collections.Generic;
using UnityEngine;

namespace Turnroot.Utilities
{
    /// <summary>
    /// Object pool for dictionaries to reduce allocations in hot paths.
    /// Particularly useful for pathfinding algorithms that create many temporary dictionaries.
    /// </summary>
    public static class DictionaryPool<TKey, TValue>
    {
        private static readonly Stack<Dictionary<TKey, TValue>> _pool = new();
        private static readonly object _lock = new();
        private const int MaxPoolSize = 20;

        /// <summary>
        /// Get a dictionary from the pool or create a new one.
        /// </summary>
        public static Dictionary<TKey, TValue> Get()
        {
            lock (_lock)
            {
                if (_pool.Count > 0)
                {
                    var dict = _pool.Pop();
                    // Clear inside lock to avoid race condition
                    dict.Clear();
                    return dict;
                }

                // Create new dictionary inside lock for consistency
                return new Dictionary<TKey, TValue>();
            }
        }

        /// <summary>
        /// Return a dictionary to the pool for reuse.
        /// </summary>
        public static void Return(Dictionary<TKey, TValue> dictionary)
        {
            if (dictionary == null)
            {
                return;
            }

            lock (_lock)
            {
                if (_pool.Count < MaxPoolSize)
                {
                    dictionary.Clear();
                    _pool.Push(dictionary);
                }
            }
        }

        /// <summary>
        /// Clear all pooled dictionaries.
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _pool.Clear();
            }
        }

        /// <summary>
        /// Get current pool size for diagnostics.
        /// </summary>
        public static int PoolSize
        {
            get
            {
                lock (_lock)
                {
                    return _pool.Count;
                }
            }
        }
    }

    /// <summary>
    /// Scoped dictionary wrapper that automatically returns the dictionary to the pool when disposed.
    /// Use with 'using' statement for automatic cleanup.
    /// </summary>
    public struct PooledDictionary<TKey, TValue> : System.IDisposable
    {
        private Dictionary<TKey, TValue> _dictionary;
        private bool _disposed;

        public Dictionary<TKey, TValue> Dictionary => _dictionary;

        public static PooledDictionary<TKey, TValue> Get()
        {
            return new PooledDictionary<TKey, TValue>
            {
                _dictionary = DictionaryPool<TKey, TValue>.Get(),
                _disposed = false,
            };
        }

        public void Dispose()
        {
            if (!_disposed && _dictionary != null)
            {
                DictionaryPool<TKey, TValue>.Return(_dictionary);
                _dictionary = null;
                _disposed = true;
            }
        }
    }
}
