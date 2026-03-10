using UnityEngine;

namespace Turnroot.Utilities.AbstractScripts
{
    /// <summary>
    /// Generic singleton pattern for ScriptableObject-derived classes.
    /// Ensures only one instance exists and provides global access.
    /// </summary>
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
                    // If not found at the exact path, search all Resources for the type.
                    _instance =
                        Resources.Load<T>(typeof(T).Name) ?? TryLoadFromResourcesWithFallback();
                }
                return _instance;
            }
        }

        private static T TryLoadFromResourcesWithFallback()
        {
            // Wrap in try/catch because Unity throws when resources are being loaded
            // (recursive serialization/Dereferencing PPtr) and this can happen when
            // ScriptableObjects reference each other during deserialization.
            try
            {
                return TryLoadFromResourcesAll();
            }
            catch (System.Exception)
            {
#if UNITY_EDITOR
                return TryLoadFromAssetDatabase();
#else
                return null;
#endif
            }
        }

        private static T TryLoadFromResourcesAll()
        {
            var all = Resources.LoadAll<T>("");
            if (all == null || all.Length == 0)
            {
                return null;
            }

            // Prefer an asset whose filename matches the type name
            foreach (var candidate in all)
            {
                if (candidate != null && candidate.name == typeof(T).Name)
                {
                    return candidate;
                }
            }

            // Otherwise just take the first one found
            return all[0];
        }

#if UNITY_EDITOR
        private static T TryLoadFromAssetDatabase()
        {
            // As a safe fallback in the editor, perform an AssetDatabase search
            // which avoids dereferencing PPtr during Resources.LoadAll.
            try
            {
                string filter = $"t:{typeof(T).Name}";
                var guids = UnityEditor.AssetDatabase.FindAssets(filter);
                if (guids == null || guids.Length == 0)
                {
                    return null;
                }

                // Prefer exact type-named asset in Resources folder if present
                var foundInstance = TryFindInResourcesFolder(guids);
                if (foundInstance != null)
                {
                    return foundInstance;
                }

                // Fallback to first found asset
                var fallbackPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(fallbackPath);
            }
            catch
            {
                return null;
            }
        }

        private static T TryFindInResourcesFolder(string[] guids)
        {
            foreach (var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (!path.Contains($"/Resources/"))
                {
                    continue;
                }

                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    return asset;
                }
            }

            return null;
        }
#endif

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

                (
                    $"SingletonScriptableObject: DUPLICATE DETECTED! Only one instance of {typeof(T).Name} is allowed. "
                    + $"Keeping: {instancePath}. "
                    + $"Deleting duplicate: {duplicatePath}"
                ).LogError();

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
}
