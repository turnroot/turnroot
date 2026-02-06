using System.Collections.Generic;
using UnityEngine;

namespace Turnroot.Utilities.AbstractScripts
{
    /// <summary>
    /// Generic object pool implementation for Unity Component-derived objects with automatic growth.
    /// </summary>
    public class ObjectPool<T>
        where T : Component
    {
        private readonly Stack<T> _pooledObjects = new();
        private readonly T _prefab;
        private readonly Transform _parent;

        // Constructor to set up the pool with a prefab and an initial size
        public ObjectPool(T prefab, int initialSize, Transform parent = null)
        {
            _prefab = prefab;
            _parent = parent;
            for (int i = 0; i < initialSize; i++)
            {
                T newObj = CreateNewObject(false);
                _pooledObjects.Push(newObj);
            }
        }

        private T CreateNewObject(bool active = true)
        {
            T newObj = Object.Instantiate(_prefab, _parent);
            newObj.gameObject.SetActive(active);
            return newObj;
        }

        // Call this to get an object instance
        public T Get()
        {
            T obj;
            if (_pooledObjects.Count > 0)
            {
                obj = _pooledObjects.Pop();
                obj.gameObject.SetActive(true);
            }
            else
            {
                // If the pool runs out, create a new one and grow the pool
#if UNITY_EDITOR
                TurnrootLogger.Log(
                    $"Pool of type {typeof(T).Name} is growing. Consider increasing initial size.",
                    TurnrootLogger.LogLevel.Warning
                );
#endif
                obj = CreateNewObject(true);
            }
            return obj;
        }

        // Call this to return an object to the pool
        public void Release(T obj)
        {
            obj.gameObject.SetActive(false);
            obj.transform.SetParent(_parent); // Reset parent in case it changed
            _pooledObjects.Push(obj);
        }
    }
}
