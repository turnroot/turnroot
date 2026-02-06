using System.Collections.Generic;

namespace Turnroot.Utilities
{
    /// <summary>
    /// Object pool for lists to reduce allocations in hot paths.
    /// Particularly useful for pathfinding algorithms that create many temporary lists.
    /// </summary>
    public static class ListPool<T>
    {
        private static readonly Stack<List<T>> _pool = new();
        private static readonly object _lock = new();
        private const int MaxPoolSize = 20;

        /// <summary>
        /// Get a list from the pool or create a new one.
        /// </summary>
        public static List<T> Get()
        {
            lock (_lock)
            {
                if (_pool.Count > 0)
                {
                    var list = _pool.Pop();
                    list.Clear();
                    return list;
                }

                return new List<T>();
            }
        }

        /// <summary>
        /// Return a list to the pool for reuse.
        /// </summary>
        public static void Return(List<T> list)
        {
            if (list == null)
            {
                return;
            }

            lock (_lock)
            {
                if (_pool.Count < MaxPoolSize)
                {
                    list.Clear();
                    _pool.Push(list);
                }
            }
        }

        /// <summary>
        /// Clear all pooled lists.
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
    /// Scoped list wrapper that automatically returns the list to the pool when disposed.
    /// Use with 'using' statement for automatic cleanup.
    /// </summary>
    public struct PooledList<T> : System.IDisposable
    {
        private bool _disposed;

        public List<T> List { get; private set; }

        public static PooledList<T> Get() => new() { List = ListPool<T>.Get(), _disposed = false };

        public void Dispose()
        {
            if (!_disposed && List != null)
            {
                ListPool<T>.Return(List);
                List = null;
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Object pool for HashSets to reduce allocations in hot paths.
    /// Useful for pathfinding closed sets and visited tracking.
    /// </summary>
    public static class HashSetPool<T>
    {
        private static readonly Stack<HashSet<T>> _pool = new();
        private static readonly object _lock = new();
        private const int MaxPoolSize = 20;

        /// <summary>
        /// Get a HashSet from the pool or create a new one.
        /// </summary>
        public static HashSet<T> Get()
        {
            lock (_lock)
            {
                if (_pool.Count > 0)
                {
                    var set = _pool.Pop();
                    set.Clear();
                    return set;
                }

                return new HashSet<T>();
            }
        }

        /// <summary>
        /// Return a HashSet to the pool for reuse.
        /// </summary>
        public static void Return(HashSet<T> set)
        {
            if (set == null)
            {
                return;
            }

            lock (_lock)
            {
                if (_pool.Count < MaxPoolSize)
                {
                    set.Clear();
                    _pool.Push(set);
                }
            }
        }

        /// <summary>
        /// Clear all pooled HashSets.
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
    /// Scoped HashSet wrapper that automatically returns the HashSet to the pool when disposed.
    /// Use with 'using' statement for automatic cleanup.
    /// </summary>
    public struct PooledHashSet<T> : System.IDisposable
    {
        private bool _disposed;

        public HashSet<T> HashSet { get; private set; }

        public static PooledHashSet<T> Get() => new() { HashSet = HashSetPool<T>.Get(), _disposed = false };

        public void Dispose()
        {
            if (!_disposed && HashSet != null)
            {
                HashSetPool<T>.Return(HashSet);
                HashSet = null;
                _disposed = true;
            }
        }
    }
}
