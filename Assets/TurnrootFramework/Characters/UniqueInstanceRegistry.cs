using System.Collections.Generic;
using Turnroot.Utilities;
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
            if (!ValidationHelper.ValidateNotNull(template, nameof(template)))
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
            if (
                !ValidationHelper.ValidateNotNull(template, nameof(template))
                || !ValidationHelper.ValidateNotNull(instance, nameof(instance))
            )
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
            if (!ValidationHelper.ValidateNotNull(template, nameof(template)))
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
