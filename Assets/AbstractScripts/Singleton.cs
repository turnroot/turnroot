using UnityEngine;

// T must be a class derived from Component
// The 'new()' constraint ensures the type T has a parameterless constructor
public abstract class Singleton<T> : MonoBehaviour
    where T : Component
{
    private static T _instance;

    // Static property for global access to the single instance
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                // 1. Try to find an existing instance in the scene
                _instance = FindFirstObjectByType<T>();

                if (_instance == null)
                {
                    // 2. If no instance exists, create a new GameObject for it
                    GameObject obj = new()
                    {
                        name = typeof(T).Name, // Naming the GameObject after the class
                    };
                    _instance = obj.AddComponent<T>();
                }
            }
            return _instance;
        }
    }

    // Ensures the instance persists across scene loads
    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(this.gameObject); // Make it persistent
        }
        else
        {
            // If an instance already exists, destroy this duplicate
            Destroy(gameObject);
        }
    }
}
