using System.Collections.Generic;
using UnityEngine;

namespace Turnroot.Characters
{
    /// <summary>
    /// Simple local registry to ensure a single runtime instance exists for templates marked as unique.
    /// This prevents accidental duplicate instances being created for templates with IsUnique==true.
    /// Note: registry is in-memory only and not persisted.
    /// </summary>
    public static class UniqueInstanceRegistry
    {
        private static readonly Dictionary<ScriptableObject, object> _map = new();
        private static readonly object _lock = new();

        public static T Get<T>(ScriptableObject template)
            where T : class
        {
            if (template == null)
            {
                return null;
            }

            lock (_lock)
            {
                return _map.TryGetValue(template, out var obj) ? obj as T : null;
            }
        }

        public static void Register(ScriptableObject template, object instance)
        {
            if (template == null || instance == null)
            {
                return;
            }

            lock (_lock)
            {
                _map[template] = instance;
            }
        }

        public static bool TryUnregister(ScriptableObject template, object instance)
        {
            if (template == null)
            {
                return false;
            }

            lock (_lock)
            {
                if (!_map.TryGetValue(template, out var existing))
                {
                    return false;
                }

                if (!ReferenceEquals(existing, instance))
                {
                    return false;
                }

                _map.Remove(template);
                return true;
            }
        }

        public static void ClearAll()
        {
            lock (_lock)
            {
                _map.Clear();
            }
        }
    }
}
