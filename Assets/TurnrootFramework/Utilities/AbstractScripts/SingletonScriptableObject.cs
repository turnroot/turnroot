using UnityEngine;

public abstract class SingletonScriptableObject<T> : ScriptableObject
    where T : ScriptableObject
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try direct load by expected name first
                _instance = Resources.Load<T>(typeof(T).Name);

                // If not found at the exact path, search all Resources for the type
                if (_instance == null)
                {
                    var all = Resources.LoadAll<T>("");
                    if (all != null && all.Length > 0)
                    {
                        // Prefer an asset whose filename matches the type name
                        foreach (var candidate in all)
                        {
                            if (candidate != null && candidate.name == typeof(T).Name)
                            {
                                _instance = candidate;
                                break;
                            }
                        }
                        // Otherwise just take the first one found
                        if (_instance == null)
                        {
                            _instance = all[0];
                        }

#if UNITY_EDITOR
                        Debug.LogWarning(
                            $"SingletonScriptableObject: Loaded {typeof(T).Name} from Resources via fallback (found {all.Length} candidates). Consider placing the asset at 'Assets/Resources/{typeof(T).Name}.asset' for deterministic loading."
                        );
#endif
                    }
                }

                if (_instance == null)
                {
#if UNITY_EDITOR
                    Debug.LogError(
                        $"SingletonScriptableObject: Could not find instance of {typeof(T).Name} in Resources."
                    );
#endif
                }
            }
            return _instance;
        }
    }

    protected virtual void OnEnable()
    {
        if (_instance == null)
        {
            _instance = this as T;
        }
        else if (_instance != this)
        {
#if UNITY_EDITOR
            // In editor, we can't destroy assets with DestroyImmediate.
            // Instead, delete the duplicate asset file to enforce singleton behavior.
            string duplicatePath = UnityEditor.AssetDatabase.GetAssetPath(this);
            string instancePath = UnityEditor.AssetDatabase.GetAssetPath(_instance);

            Debug.LogError(
                $"SingletonScriptableObject: DUPLICATE DETECTED! Only one instance of {typeof(T).Name} is allowed. "
                    + $"Keeping: {instancePath}. "
                    + $"Deleting duplicate: {duplicatePath}",
                _instance
            );

            // Delete the duplicate asset file
            if (!string.IsNullOrEmpty(duplicatePath))
            {
                UnityEditor.AssetDatabase.DeleteAsset(duplicatePath);
                UnityEditor.AssetDatabase.Refresh();
            }
#else
            // At runtime, destroy the duplicate instance
            DestroyImmediate(this);
#endif
        }
    }

    protected virtual void OnDisable()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
