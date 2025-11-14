using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Turnroot.Utilities
{
    /// <summary>
    /// Helper to load game settings assets placed under Resources/GameSettings/*.
    /// Tries a direct `Resources.Load` using the type name, then `Resources.LoadAll` on the
    /// subfolder, and finally an Editor `AssetDatabase` search as a fallback.
    /// </summary>
    public static class GameSettingsLoader
    {
        public static T LoadFirst<T>(string subfolder = "GameSettings")
            where T : ScriptableObject
        {
            // Prefer an exact-named asset matching the type name under the subfolder
            string typeName = typeof(T).Name;
            T found = Resources.Load<T>($"{subfolder}/{typeName}");
            if (found != null)
                return found;

            // Fallback: load any asset of this type under the subfolder
            var foundAll = Resources.LoadAll<T>(subfolder);
            if (foundAll != null && foundAll.Length > 0)
                return foundAll[0];

#if UNITY_EDITOR
            // Editor-only fallback: search AssetDatabase for an asset under Resources/<subfolder>/
            try
            {
                string filter = $"t:{typeName}";
                var guids = AssetDatabase.FindAssets(filter);
                if (guids != null && guids.Length > 0)
                {
                    // Prefer assets explicitly under a Resources/<subfolder> directory
                    foreach (var g in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(g);
                        if (path.Contains($"/Resources/{subfolder}/"))
                        {
                            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                            if (asset != null)
                                return asset;
                        }
                    }

                    // Otherwise return the first found of this type in the project
                    var fallbackPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    return AssetDatabase.LoadAssetAtPath<T>(fallbackPath);
                }
            }
            catch { }
#endif

            return null;
        }
    }
}
