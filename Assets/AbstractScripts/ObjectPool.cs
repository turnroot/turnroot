using System.Collections.Generic;
using UnityEngine;

// T must be a class derived from Component
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
        T newObj = GameObject.Instantiate(_prefab, _parent);
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
            Debug.LogWarning(
                $"Pool of type {typeof(T).Name} is growing. Consider increasing initial size."
            );
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

/* // --- Example Usage (requires a MonoBehavior wrapper for scene setup) ---
public class BulletPoolManager : Singleton<BulletPoolManager>
{
    [SerializeField] private Bullet bulletPrefab;
    private ObjectPool<Bullet> _bulletPool;
    
    protected override void Awake()
    {
        base.Awake();
        // Initialize the pool with a size of 50
        _bulletPool = new ObjectPool<Bullet>(bulletPrefab, 50, transform);
    }
    
    public Bullet SpawnBullet(Vector3 position, Quaternion rotation)
    {
        Bullet b = _bulletPool.Get();
        b.transform.position = position;
        b.transform.rotation = rotation;
        // Bullet script handles calling BulletPoolManager.Instance.ReturnBullet(this); on hit/time out
        return b;
    }
    
    public void ReturnBullet(Bullet bullet)
    {
        _bulletPool.Release(bullet);
    }
}
*/
