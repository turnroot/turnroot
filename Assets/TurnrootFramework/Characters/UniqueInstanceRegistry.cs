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

        public static T Get<T>(ScriptableObject template)
            where T : class
        {
            if (template == null)
                return null;
            if (_map.TryGetValue(template, out var obj))
                return obj as T;
            return null;
        }

        public static void Register(ScriptableObject template, object instance)
        {
            if (template == null || instance == null)
                return;
            _map[template] = instance;
        }

        public static bool TryUnregister(ScriptableObject template, object instance)
        {
            if (template == null)
                return false;
            if (!_map.TryGetValue(template, out var existing))
                return false;
            if (!ReferenceEquals(existing, instance))
                return false;
            _map.Remove(template);
            return true;
        }

        public static void ClearAll() => _map.Clear();
    }
}
